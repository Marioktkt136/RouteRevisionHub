using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using RouteRevisionHub.Data;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Services;

public record SyncScanReportDto(
    int ScannedFilesOnDisk,
    int NewResourcesAdded,
    int NewModulesCreated,
    int NewTopicsCreated,
    int NewTopicLinksCreated,
    List<string> NewFilePaths,
    List<string> CreatedTopicTitles);

/// <summary>
/// Scans the configured Google Drive Root Folders using the signed-in user's OAuth token,
/// extracts PPTX/DOCX/ZIP/code/SQL text in memory for newly added files, matches them to existing topics
/// or auto-scaffolds new Modules/Topics cleanly.
/// </summary>
public partial class CourseSyncService
{
    private readonly AppDbContext _db;
    private readonly GoogleDriveService _drive;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<CourseSyncService> _logger;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".webm", ".mov", ".avi", ".m4a", ".mp3", ".wav",
        ".pptx", ".pdf", ".docx", ".html", ".htm",
        ".sql", ".cs", ".csproj", ".slnx", ".sln", ".cshtml",
        ".zip", ".rar", ".bak", ".txt", ".md"
    };

    private static readonly HashSet<string> IgnoredSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".vs", ".git", "node_modules", "packages", "__macosx"
    };

    public CourseSyncService(
        AppDbContext db,
        GoogleDriveService drive,
        IHttpContextAccessor http,
        ILogger<CourseSyncService> logger)
    {
        _db = db;
        _drive = drive;
        _http = http;
        _logger = logger;
    }

    public async Task<SyncScanReportDto> ScanAndSyncAsync(bool autoCreateTopics = true, CancellationToken ct = default)
    {
        var ctx = _http.HttpContext ?? throw new InvalidOperationException("No active HTTP context.");
        string? accessToken = await _drive.GetValidUserAccessTokenAsync(ctx, ct);
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new UnauthorizedAccessException("Google OAuth access token is missing or expired. Please sign in again.");

        var opts = _drive.GetDriveOptions();
        var driveFiles = new List<(DriveApiFileDto File, string ParentId, string RelPath, string RootLabel, string Role)>();
        var visitedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var frontier = new List<(string FolderId, string CurrentPath, string RootLabel, string Role)>();

        foreach (var root in opts.RootFolders)
        {
            if (!string.IsNullOrWhiteSpace(root.FolderId) && visitedFolders.Add(root.FolderId))
                frontier.Add((root.FolderId, root.Label, root.Label, root.Role));
        }

        using var gate = new SemaphoreSlim(5);
        while (frontier.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var batch = frontier.ToList();
            frontier.Clear();

            var results = await Task.WhenAll(batch.Select(async item =>
            {
                await gate.WaitAsync(ct);
                try
                {
                    var children = await _drive.ListFolderChildrenAsync(accessToken, item.FolderId, ct);
                    return (Item: item, Children: (List<DriveApiFileDto>?)children);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Skipping Drive folder {FolderId} ({Path}) during sync due to permissions/HTTP error.", item.FolderId, item.CurrentPath);
                    return (Item: item, Children: (List<DriveApiFileDto>?)null);
                }
                finally
                {
                    gate.Release();
                }
            }));

            foreach (var res in results)
            {
                if (res.Children is null) continue;
                foreach (var ch in res.Children)
                {
                    string childPath = $"{res.Item.CurrentPath}/{ch.Name}";
                    string effectiveMime = ch.MimeType;
                    string effectiveId = ch.Id;

                    if (effectiveMime == "application/vnd.google-apps.shortcut" && ch.ShortcutDetails?.TargetId is not null)
                    {
                        effectiveId = ch.ShortcutDetails.TargetId;
                        effectiveMime = ch.ShortcutDetails.TargetMimeType ?? effectiveMime;
                    }

                    if (effectiveMime == "application/vnd.google-apps.folder")
                    {
                        if (!IgnoredSegments.Contains(ch.Name.Trim()) && visitedFolders.Add(effectiveId))
                            frontier.Add((effectiveId, childPath, res.Item.RootLabel, res.Item.Role));
                        continue;
                    }

                    string ext = Path.GetExtension(ch.Name);
                    if (!SupportedExtensions.Contains(ext))
                        continue;

                    driveFiles.Add((new DriveApiFileDto
                    {
                        Id = effectiveId,
                        Name = ch.Name,
                        MimeType = effectiveMime,
                        SizeString = ch.SizeString,
                        ModifiedTime = ch.ModifiedTime,
                        WebViewLink = ch.WebViewLink ?? $"https://drive.google.com/file/d/{effectiveId}/view"
                    }, res.Item.FolderId, childPath, res.Item.RootLabel, res.Item.Role));
                }
            }
        }

        var existingDriveIds = new HashSet<string>(
            await _db.CourseResources.AsNoTracking().Where(r => r.DriveFileId != null).Select(r => r.DriveFileId!).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        var existingRelPaths = new HashSet<string>(
            await _db.CourseResources.AsNoTracking().Select(r => r.RelativePath).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        var newResources = new List<CourseResource>();
        var newFilePaths = new List<string>();

        foreach (var item in driveFiles)
        {
            ct.ThrowIfCancellationRequested();
            if (existingDriveIds.Contains(item.File.Id)) continue;

            string relPath = MakeUniqueRelPath(item.RelPath, item.File.Id, existingRelPaths);
            existingDriveIds.Add(item.File.Id);
            existingRelPaths.Add(relPath);

            var resource = await BuildResourceFromDriveAsync(
                accessToken, item.File, item.ParentId, relPath, item.RootLabel, item.Role, ct);
            newResources.Add(resource);
            newFilePaths.Add(relPath);
        }

        if (newResources.Count > 0)
        {
            _db.CourseResources.AddRange(newResources);
            await _db.SaveChangesAsync(ct);
        }

        int modulesCreated = 0;
        int topicsCreated = 0;
        int linksCreated = 0;
        var createdTopicTitles = new List<string>();

        if (newResources.Count > 0)
        {
            var modules = await _db.TrackModules
                .Include(m => m.Topics).ThenInclude(t => t.ConventionKeys)
                .Include(m => m.Topics).ThenInclude(t => t.SourceRefs)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync(ct);

            var allTopics = modules.SelectMany(m => m.Topics).ToList();
            var unlinkedNew = new List<CourseResource>();

            foreach (var res in newResources)
            {
                bool matchedAny = false;
                string relLow = res.RelativePath.ToLowerInvariant();
                string searchLow = res.SearchableText.ToLowerInvariant();

                foreach (var topic in allTopics)
                {
                    int score = ComputeTopicMatchScore(topic, res, relLow, searchLow);
                    if (score < 28) continue;

                    if (topic.SourceRefs.Any(s => s.CourseResourceId == res.Id)) continue;

                    var link = new TopicSourceRef
                    {
                        TopicId = topic.Id,
                        CourseResourceId = res.Id,
                        RelevanceScore = score,
                        MatchReason = "Auto-linked on Live Google Drive Sync",
                        MatchedLocations = ExtractFirstMatchedLocation(res),
                        MatchedExcerpt = BuildExcerpt(res.SearchableText),
                        IsPrimary = score >= 55
                    };
                    _db.TopicSourceRefs.Add(link);
                    topic.SourceRefs.Add(link);
                    linksCreated++;
                    matchedAny = true;

                    if (topic.TrackModule.IsUpcoming)
                    {
                        topic.TrackModule.IsUpcoming = false;
                        if (topic.Priority == "Upcoming") topic.Priority = "Core";
                    }
                }

                if (!matchedAny)
                    unlinkedNew.Add(res);
            }

            if (autoCreateTopics && unlinkedNew.Count > 0)
            {
                var groups = unlinkedNew.GroupBy(r => (r.ModuleFolder, r.SessionName)).ToList();
                int maxModOrder = modules.Count > 0 ? modules.Max(m => m.DisplayOrder) : 0;
                var existingSlugs = new HashSet<string>(allTopics.Select(t => t.Slug), StringComparer.OrdinalIgnoreCase);

                foreach (var grp in groups)
                {
                    string modFolder = grp.Key.ModuleFolder;
                    string sessionName = grp.Key.SessionName;

                    var targetModule = FindMatchingModule(modules, modFolder);
                    if (targetModule is null)
                    {
                        maxModOrder++;
                        string code = ExtractCodePrefix(modFolder, maxModOrder);
                        while (modules.Any(m => m.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
                            code = $"{code}N";

                        string cleanModTitle = CleanFolderDisplayTitle(modFolder);
                        targetModule = new TrackModule
                        {
                            Code = Trunc(code, 24),
                            TitleEn = Trunc(cleanModTitle, 160),
                            TitleAr = Trunc($"موديول {cleanModTitle} (مضاف تلقائياً من Drive)", 160),
                            Description = $"Auto-discovered module from Google Drive folder '{modFolder}'.",
                            DisplayOrder = maxModOrder,
                            BadgeColor = "#22d3ee",
                            IsUpcoming = false
                        };
                        _db.TrackModules.Add(targetModule);
                        await _db.SaveChangesAsync(ct);
                        modules.Add(targetModule);
                        modulesCreated++;
                    }
                    else if (targetModule.IsUpcoming)
                    {
                        targetModule.IsUpcoming = false;
                    }

                    string cleanTopicTitle = string.IsNullOrWhiteSpace(sessionName) || sessionName == modFolder
                        ? CleanFolderDisplayTitle(modFolder)
                        : $"{CleanFolderDisplayTitle(modFolder)} — {CleanFolderDisplayTitle(sessionName)}";

                    var autoTopic = targetModule.Topics.FirstOrDefault(t =>
                        t.TitleEn.Equals(cleanTopicTitle, StringComparison.OrdinalIgnoreCase)
                        || (!string.IsNullOrWhiteSpace(sessionName) && t.SessionLabel.Equals(sessionName, StringComparison.OrdinalIgnoreCase)));

                    bool isNewTopic = autoTopic is null;
                    if (isNewTopic)
                    {
                        string baseSlug = Slugify(cleanTopicTitle);
                        string slug = baseSlug;
                        int suffix = 2;
                        while (existingSlugs.Contains(slug)) slug = $"{baseSlug}-{suffix++}";
                        existingSlugs.Add(slug);

                        int nextTopicOrder = (targetModule.Topics.Count > 0 ? targetModule.Topics.Max(t => t.DisplayOrder) : 0) + 1;
                        autoTopic = new Topic
                        {
                            TrackModuleId = targetModule.Id,
                            Slug = Trunc(slug, 140),
                            TitleEn = Trunc(cleanTopicTitle, 200),
                            TitleAr = Trunc($"مراجعة: {cleanTopicTitle}", 200),
                            SessionLabel = Trunc(sessionName, 100),
                            DisplayOrder = nextTopicOrder,
                            Priority = "Core",
                            SummaryAr = $"تم اكتشاف هذا الموضوع تلقائياً من Google Drive ({modFolder}/{sessionName}).",
                            SummaryEn = $"Auto-discovered from newly uploaded Google Drive files ({grp.Count()} file(s) in {modFolder}/{sessionName}).",
                            CodeLanguage = grp.Any(r => r.ResourceType == "SQL") ? "sql" : "csharp",
                            CommonMistakes = "Review newly added session demos and add key traps here.",
                            InterviewQuestions = $"Summarize the main concepts covered in {cleanTopicTitle}."
                        };
                    }

                    foreach (var r in grp)
                    {
                        if (autoTopic!.SourceRefs.Any(s => s.CourseResourceId == r.Id)) continue;
                        autoTopic.SourceRefs.Add(new TopicSourceRef
                        {
                            CourseResourceId = r.Id,
                            RelevanceScore = 80,
                            MatchReason = "Auto-discovered Drive Topic Resource",
                            MatchedLocations = ExtractFirstMatchedLocation(r),
                            IsPrimary = r.ResourceType is "Video" or "Presentation"
                        });
                        linksCreated++;
                    }

                    if (isNewTopic)
                    {
                        _db.Topics.Add(autoTopic!);
                        targetModule.Topics.Add(autoTopic!);
                        topicsCreated++;
                        createdTopicTitles.Add(autoTopic!.TitleEn);
                    }
                }
            }

            await RecalculateModuleCountersAsync(ct);
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Drive sync complete: scanned={Scanned}, newRes={Res}, newMod={Mod}, newTopic={Top}, newLinks={Links}",
            driveFiles.Count, newResources.Count, modulesCreated, topicsCreated, linksCreated);

        return new SyncScanReportDto(
            driveFiles.Count,
            newResources.Count,
            modulesCreated,
            topicsCreated,
            linksCreated,
            newFilePaths.Take(50).ToList(),
            createdTopicTitles);
    }

    public async Task RecalculateModuleCountersAsync(CancellationToken ct = default)
    {
        var resources = await _db.CourseResources.AsNoTracking().ToListAsync(ct);
        var modules = await _db.TrackModules.ToListAsync(ct);

        foreach (var mod in modules)
        {
            if (mod.IsUpcoming) continue;
            var matched = resources.Where(r => DoesFolderBelongToModule(mod, r.ModuleFolder, r.DriveRootLabel)).ToList();
            if (matched.Count == 0) continue;

            mod.TotalFileCount = matched.Count;
            mod.TotalVideoCount = matched.Count(r => r.ResourceType is "Video" or "Audio");
            mod.TotalVideoHours = Math.Round(matched.Where(r => r.ResourceType is "Video" or "Audio").Sum(r => r.DurationSeconds) / 3600m, 2);
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task<CourseResource> BuildResourceFromDriveAsync(
        string accessToken,
        DriveApiFileDto file,
        string parentId,
        string relPath,
        string rootLabel,
        string role,
        CancellationToken ct)
    {
        string ext = Path.GetExtension(file.Name).ToLowerInvariant();
        string resType = ext switch
        {
            ".mp4" or ".mkv" or ".webm" or ".mov" or ".avi" => "Video",
            ".m4a" or ".mp3" or ".wav" => "Audio",
            ".pptx" => "Presentation",
            ".pdf" => "PDF",
            ".docx" => "Guide",
            ".html" or ".htm" => "HTML",
            ".sql" => "SQL",
            ".cs" or ".csproj" or ".slnx" or ".sln" or ".cshtml" => "Code",
            ".zip" or ".rar" or ".bak" => "Archive",
            _ => "Text"
        };

        string modFolder = DetectModuleFolderFromPath(relPath, rootLabel, role);
        string instructor = rootLabel == "Eng Hossam Mostafa" ? "Hossam Mostafa" : rootLabel;
        string session = DetectSessionFromPath(relPath);
        string webLink = file.WebViewLink ?? $"https://drive.google.com/file/d/{file.Id}/view";

        var structured = new List<object>();
        var sb = new StringBuilder();
        sb.AppendLine(file.Name).AppendLine(relPath).AppendLine(instructor).AppendLine(session);
        int pageOrSlideCount = 0;

        bool shouldExtractContent = ext is ".pptx" or ".docx" or ".zip" or ".cs" or ".sql" or ".txt" or ".md" or ".csproj" or ".cshtml" or ".html" or ".htm"
            && (file.SizeBytes <= 12 * 1024 * 1024);

        if (shouldExtractContent)
        {
            try
            {
                var bytes = await _drive.DownloadSmallFileBytesAsync(accessToken, file.Id, 12 * 1024 * 1024, ct);
                if (bytes is not null)
                    ExtractTextInMemory(bytes, ext, sb, structured, ref pageOrSlideCount);
            }
            catch
            {
                // Non-fatal: still catalog Drive metadata cleanly
            }
        }

        return new CourseResource
        {
            RelativePath = Trunc(relPath, 420),
            FullPath = Trunc(webLink, 520),
            FileName = Trunc(file.Name, 220),
            Extension = Trunc(ext, 16),
            ResourceType = Trunc(resType, 32),
            ModuleFolder = Trunc(modFolder, 80),
            Instructor = Trunc(instructor, 80),
            SessionName = Trunc(session, 120),
            DurationSeconds = 0,
            DurationFormatted = "",
            SizeMb = Math.Round((decimal)(file.SizeBytes / (1024.0 * 1024.0)), 2),
            PageOrSlideCount = pageOrSlideCount,
            SearchableText = Trunc(sb.ToString(), 120000),
            StructuredPreviewJson = structured.Count > 0 ? JsonSerializer.Serialize(structured.Take(80)) : null,
            DriveFileId = Trunc(file.Id, 120),
            DriveParentFolderId = Trunc(parentId, 120),
            DriveWebViewLink = Trunc(webLink, 300),
            DriveMimeType = Trunc(file.MimeType, 150),
            DriveRootLabel = Trunc(rootLabel, 80)
        };
    }

    private static void ExtractTextInMemory(byte[] bytes, string ext, StringBuilder sb, List<object> structured, ref int count)
    {
        using var ms = new MemoryStream(bytes, writable: false);
        if (ext == ".pptx")
        {
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
            var slides = zip.Entries
                .Where(e => Regex.IsMatch(e.FullName, @"^ppt/slides/slide\d+\.xml$", RegexOptions.IgnoreCase))
                .Select(e => (Num: int.Parse(Regex.Match(e.FullName, @"slide(\d+)\.xml").Groups[1].Value), Entry: e))
                .OrderBy(x => x.Num)
                .ToList();
            count = slides.Count;
            foreach (var (num, entry) in slides)
            {
                using var stream = entry.Open();
                var doc = XDocument.Load(stream);
                string txt = string.Join(" ", doc.Descendants().Where(x => x.Name.LocalName == "t").Select(x => x.Value.Trim()).Where(v => v.Length > 0));
                if (!string.IsNullOrWhiteSpace(txt))
                {
                    structured.Add(new { label = $"Slide {num:D2}", t = num, text = Trunc(txt, 2500) });
                    sb.AppendLine($"[S{num:D2}] {txt}");
                }
            }
        }
        else if (ext == ".docx")
        {
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
            var docEntry = zip.GetEntry("word/document.xml");
            if (docEntry is not null)
            {
                using var stream = docEntry.Open();
                var doc = XDocument.Load(stream);
                string txt = string.Join("\n", doc.Descendants()
                    .Where(x => x.Name.LocalName == "p")
                    .Select(p => string.Concat(p.Descendants().Where(x => x.Name.LocalName == "t").Select(x => x.Value)))
                    .Where(l => !string.IsNullOrWhiteSpace(l)));
                sb.AppendLine(Trunc(txt, 35000));
            }
        }
        else if (ext == ".zip")
        {
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
            var codeEntries = zip.Entries
                .Where(e => e.Length > 0 && e.Length <= 250_000 &&
                            !e.FullName.Contains("/bin/", StringComparison.OrdinalIgnoreCase) &&
                            !e.FullName.Contains("/obj/", StringComparison.OrdinalIgnoreCase) &&
                            Path.GetExtension(e.Name).ToLowerInvariant() is ".cs" or ".sql" or ".cshtml" or ".md" or ".txt")
                .Take(35)
                .ToList();
            count = codeEntries.Count;
            foreach (var ce in codeEntries)
            {
                using var sr = new StreamReader(ce.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                string txt = sr.ReadToEnd();
                structured.Add(new { label = ce.FullName, t = 0, text = Trunc(txt, 4000) });
                sb.AppendLine($"[{ce.FullName}] {Trunc(txt, 4000)}");
            }
        }
        else
        {
            string raw = Encoding.UTF8.GetString(bytes);
            if (ext is ".html" or ".htm")
                raw = Regex.Replace(raw, "<[^>]+>", " ");
            sb.AppendLine(Trunc(raw, 35000));
        }
    }

    private static string DetectModuleFolderFromPath(string relPath, string rootLabel, string role)
    {
        string low = relPath.ToLowerInvariant();
        if (role == "Assignments") return "20 Assignments Explanation";
        if (role == "Workshop") return low.Contains("00 extra") ? "Course Overview" : $"21 Workshopes ({rootLabel})";
        if (low.Contains("00 orientation")) return "Course Overview";
        if (low.Contains("01 c#")) return "01 C# Basics";
        if (low.Contains("02 oop")) return "02 OOP";
        if (low.Contains("03 adv")) return "03 Advanced C#";
        if (low.Contains("04 database")) return "04 Database";
        if (low.Contains("05 linq")) return "05 LINQ";
        if (low.Contains("06 ef") || low.Contains("06 entity")) return "06 Entity Framework Core";
        if (low.Contains("07 pre")) return "07 Pre ASP.NET";
        if (low.Contains("08 asp.net")) return "08 ASP.NET Core MVC";
        return rootLabel;
    }

    private static string DetectSessionFromPath(string relPath)
    {
        var parts = relPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            if (Regex.IsMatch(p, @"session|week|workshop", RegexOptions.IgnoreCase))
                return Trunc(p.Trim(), 110);
        }
        return parts.Length > 2 ? Trunc(parts[1], 110) : (parts.Length > 0 ? Trunc(parts[0], 110) : "Drive");
    }

    private static string MakeUniqueRelPath(string rawRel, string driveId, HashSet<string> used)
    {
        string cand = Trunc(rawRel, 420);
        if (!used.Contains(cand)) return cand;
        string ext = Path.GetExtension(cand);
        string without = cand[..^ext.Length];
        return Trunc($"{Trunc(without, 400)}_{Trunc(driveId, 8)}{ext}", 420);
    }

    private static bool DoesFolderBelongToModule(TrackModule mod, string moduleFolder, string? rootLabel)
    {
        if (string.IsNullOrWhiteSpace(moduleFolder)) return false;
        string mfLow = moduleFolder.ToLowerInvariant();
        if (mod.Code == "00" && mfLow == "course overview") return true;
        if (mod.Code.Length >= 2 && mfLow.StartsWith(mod.Code.ToLowerInvariant())) return true;
        if (mod.Code == "09" && (mfLow.StartsWith("20 ") || mfLow.StartsWith("21 ") || mfLow.StartsWith("22 ")
            || rootLabel is "Aya Ali" or "Menna Elashry" or "Assignments Explanation"))
            return true;
        return false;
    }

    private static TrackModule? FindMatchingModule(List<TrackModule> modules, string modFolder)
    {
        string low = modFolder.ToLowerInvariant();
        if (low.StartsWith("20 ") || low.StartsWith("21 ") || low.StartsWith("22 "))
        {
            var capMod = modules.FirstOrDefault(m => m.Code == "09");
            if (capMod is not null) return capMod;
        }
        var codeMatch = Regex.Match(low, @"^(\d{2})");
        if (codeMatch.Success)
        {
            string c = codeMatch.Groups[1].Value;
            var byCode = modules.FirstOrDefault(m => m.Code.Equals(c, StringComparison.OrdinalIgnoreCase));
            if (byCode is not null) return byCode;
        }
        return modules.FirstOrDefault(m =>
            low.Contains(m.TitleEn.ToLowerInvariant())
            || m.TitleEn.ToLowerInvariant().Contains(CleanFolderDisplayTitle(modFolder).ToLowerInvariant()));
    }

    private static int ComputeTopicMatchScore(Topic topic, CourseResource res, string relLow, string searchLow)
    {
        int score = 0;
        if (DoesFolderBelongToModule(topic.TrackModule, res.ModuleFolder, res.DriveRootLabel))
            score += 20;

        string sess = topic.SessionLabel.ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(sess) && relLow.Contains(sess.Split('&')[0].Trim()))
            score += 25;

        var tokens = new List<string>();
        tokens.AddRange(topic.TitleEn.Split(new[] { ' ', ',', '/', ':', '(', ')', '&', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 4));
        tokens.AddRange(topic.ConventionKeys.Select(k => k.KeyName.Split('(')[0].Trim()).Where(w => w.Length >= 3));

        foreach (var tok in tokens.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string tLow = tok.ToLowerInvariant();
            if (res.FileName.Contains(tok, StringComparison.OrdinalIgnoreCase))
                score += 24;
            else if (searchLow.Contains(tLow))
                score += 6;
        }

        if (topic.Slug == "upcoming-web-api-rest-jwt-identity" &&
            (relLow.Contains("web api") || relLow.Contains("webapi") || relLow.Contains("jwt") || relLow.Contains("identity") || relLow.Contains("swagger")))
            score += 65;
        if (topic.Slug == "upcoming-clean-architecture-patterns-deployment" &&
            (relLow.Contains("clean architecture") || relLow.Contains("onion") || relLow.Contains("repository") || relLow.Contains("signalr") || relLow.Contains("docker")))
            score += 65;

        return score;
    }

    private static string ExtractFirstMatchedLocation(CourseResource res)
        => res.PageOrSlideCount > 0 ? $"{res.PageOrSlideCount} items/slides" : res.SessionName;

    private static string? BuildExcerpt(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        return text.Length > 220 ? text[..220].Replace('\r', ' ').Replace('\n', ' ') : text;
    }

    private static string ExtractCodePrefix(string modFolder, int fallbackNum)
    {
        var m = Regex.Match(modFolder, @"^(\d{2})");
        return m.Success ? m.Groups[1].Value : fallbackNum.ToString("D2");
    }

    private static string CleanFolderDisplayTitle(string name)
        => Regex.Replace(name, @"^\d{2}[\s\-_]*", "").Replace('_', ' ').Trim();

    public static string Slugify(string text)
    {
        string clean = Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(clean) ? $"topic-{Guid.NewGuid():N}"[..16] : clean;
    }

    private static string Trunc(string value, int max)
        => value.Length <= max ? value : value[..max];
}
