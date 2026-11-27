using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using RouteRevisionHub.Data.Seed;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Data;

/// <summary>
/// Migrates the MS SQL Server database on startup, seeds the Google Drive-mapped curriculum catalog,
/// template checklists, and template chapter exams, and provisions per-user starter tasks on first login.
/// </summary>
public class DatabaseSeeder
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DatabaseSeeder> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DatabaseSeeder(
        AppDbContext db,
        IConfiguration config,
        IWebHostEnvironment env,
        ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _config = config;
        _env = env;
        _logger = logger;
    }

    public async Task SeedAsync(bool forceRefresh = false)
    {
        if (_db.Database.IsNpgsql())
        {
            var creator = _db.Database.GetService<IRelationalDatabaseCreator>();
            if (!await creator.ExistsAsync())
                await creator.CreateAsync();

            if (!await NpgsqlTableExistsAsync("TrackModules"))
                await creator.CreateTablesAsync();
        }
        else
        {
            await _db.Database.MigrateAsync();
        }

        if (!forceRefresh && await _db.TrackModules.AnyAsync() && await _db.CourseResources.AnyAsync())
        {
            await SeedChecklistDefaultsAsync();
            await SeedChapterExamsDefaultsAsync();
            return;
        }

        string relSeed = (_config["CourseSettings:SeedFilePath"] ?? Path.Combine("Data", "Seed", "drive_knowledge_base.json"))
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        string fullSeedPath = Path.IsPathRooted(relSeed)
            ? relSeed
            : Path.Combine(_env.ContentRootPath, relSeed);

        string gzSeedPath = fullSeedPath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
            ? fullSeedPath
            : fullSeedPath + ".gz";

        bool useGzip = fullSeedPath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
            || (!File.Exists(fullSeedPath) && File.Exists(gzSeedPath));
        string actualSeedPath = useGzip ? gzSeedPath : fullSeedPath;

        if (!File.Exists(actualSeedPath))
        {
            _logger.LogWarning("Seed file not found at {SeedPath}. Skipping curriculum seed.", actualSeedPath);
            return;
        }

        await using var rawStream = File.OpenRead(actualSeedPath);
        await using Stream stream = useGzip
            ? new GZipStream(rawStream, CompressionMode.Decompress)
            : rawStream;
        var payload = await JsonSerializer.DeserializeAsync<SeedPayloadDto>(stream, JsonOpts);
        if (payload is null)
        {
            _logger.LogError("Failed to deserialize seed JSON from {SeedPath}.", fullSeedPath);
            return;
        }

        if (forceRefresh)
        {
            _db.QuizAttempts.RemoveRange(_db.QuizAttempts);
            _db.TopicSourceRefs.RemoveRange(_db.TopicSourceRefs);
            _db.FlashcardQuestions.RemoveRange(_db.FlashcardQuestions);
            _db.ConventionKeys.RemoveRange(_db.ConventionKeys);
            _db.TopicProgresses.RemoveRange(_db.TopicProgresses);
            _db.Topics.RemoveRange(_db.Topics);
            _db.TrackModules.RemoveRange(_db.TrackModules);
            _db.CourseResources.RemoveRange(_db.CourseResources);
            await _db.SaveChangesAsync();
        }

        var resourcesByRelPath = new Dictionary<string, CourseResource>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in payload.Resources)
        {
            if (resourcesByRelPath.ContainsKey(r.RelativePath)) continue;

            var entity = new CourseResource
            {
                RelativePath = Trunc(r.RelativePath, 420),
                FullPath = Trunc(r.FullPath, 520),
                FileName = Trunc(r.FileName, 220),
                Extension = Trunc(r.Extension, 16),
                ResourceType = Trunc(r.ResourceType, 32),
                ModuleFolder = Trunc(r.ModuleFolder, 80),
                Instructor = Trunc(r.Instructor, 80),
                SessionName = Trunc(r.SessionName, 120),
                DurationSeconds = r.DurationSeconds,
                DurationFormatted = Trunc(r.DurationFormatted, 16),
                SizeMb = r.SizeMb,
                PageOrSlideCount = r.PageOrSlideCount,
                SearchableText = r.SearchableText ?? string.Empty,
                StructuredPreviewJson = r.StructuredPreviewJson,
                DriveFileId = string.IsNullOrWhiteSpace(r.DriveFileId) ? null : Trunc(r.DriveFileId, 120),
                DriveParentFolderId = string.IsNullOrWhiteSpace(r.DriveParentFolderId) ? null : Trunc(r.DriveParentFolderId, 120),
                DriveWebViewLink = string.IsNullOrWhiteSpace(r.DriveWebViewLink) ? null : Trunc(r.DriveWebViewLink, 300),
                DriveMimeType = string.IsNullOrWhiteSpace(r.DriveMimeType) ? null : Trunc(r.DriveMimeType, 150),
                DriveRootLabel = string.IsNullOrWhiteSpace(r.DriveRootLabel) ? null : Trunc(r.DriveRootLabel, 80)
            };
            resourcesByRelPath[r.RelativePath] = entity;
            _db.CourseResources.Add(entity);
        }

        await _db.SaveChangesAsync();

        foreach (var mDto in payload.Modules)
        {
            var module = new TrackModule
            {
                Code = Trunc(mDto.Code, 24),
                TitleEn = Trunc(mDto.TitleEn, 160),
                TitleAr = Trunc(mDto.TitleAr, 160),
                Description = Trunc(mDto.Description, 1000),
                DisplayOrder = mDto.DisplayOrder,
                BadgeColor = Trunc(mDto.BadgeColor, 32),
                TotalVideoCount = mDto.TotalVideoCount,
                TotalVideoHours = mDto.TotalVideoHours,
                TotalFileCount = mDto.TotalFileCount,
                IsUpcoming = mDto.IsUpcoming
            };

            foreach (var tDto in mDto.Topics)
            {
                var topic = new Topic
                {
                    Slug = Trunc(tDto.Slug, 140),
                    TitleEn = Trunc(tDto.TitleEn, 200),
                    TitleAr = Trunc(tDto.TitleAr, 200),
                    SessionLabel = Trunc(tDto.SessionLabel, 100),
                    DisplayOrder = tDto.DisplayOrder,
                    Priority = Trunc(tDto.Priority, 32),
                    SummaryAr = tDto.SummaryAr,
                    SummaryEn = tDto.SummaryEn,
                    CodeSnippet = tDto.CodeSnippet,
                    CodeLanguage = Trunc(tDto.CodeLanguage, 24),
                    CommonMistakes = tDto.CommonMistakes,
                    InterviewQuestions = tDto.InterviewQuestions
                };

                foreach (var ck in tDto.ConventionKeys)
                {
                    topic.ConventionKeys.Add(new ConventionKey
                    {
                        KeyName = Trunc(ck.KeyName, 150),
                        Category = Trunc(ck.Category, 48),
                        ShortRule = Trunc(ck.ShortRule, 700),
                        ExampleSnippet = ck.ExampleSnippet is null ? null : Trunc(ck.ExampleSnippet, 500),
                        IsCritical = ck.IsCritical
                    });
                }

                foreach (var q in tDto.Questions)
                {
                    topic.Questions.Add(new FlashcardQuestion
                    {
                        QuestionType = Trunc(q.QuestionType, 24),
                        Prompt = Trunc(q.Prompt, 1200),
                        CodeBlock = q.CodeBlock is null ? null : Trunc(q.CodeBlock, 2000),
                        OptionsJson = q.Options.Count > 0 ? Trunc(JsonSerializer.Serialize(q.Options), 2000) : null,
                        CorrectAnswer = Trunc(q.CorrectAnswer, 600),
                        Explanation = Trunc(q.Explanation, 1200),
                        SourceCitation = Trunc(q.SourceCitation, 300)
                    });
                }

                var seenResourceIds = new HashSet<int>();
                foreach (var sr in tDto.SourceRefs)
                {
                    if (!resourcesByRelPath.TryGetValue(sr.ResourceRelativePath, out var res)) continue;
                    if (!seenResourceIds.Add(res.Id)) continue;

                    topic.SourceRefs.Add(new TopicSourceRef
                    {
                        CourseResourceId = res.Id,
                        RelevanceScore = sr.RelevanceScore,
                        MatchReason = Trunc(sr.MatchReason, 220),
                        MatchedLocations = Trunc(sr.MatchedLocations, 400),
                        FirstTimestampSeconds = sr.FirstTimestampSeconds,
                        MatchedExcerpt = sr.MatchedExcerpt is null ? null : Trunc(sr.MatchedExcerpt, 600),
                        IsPrimary = sr.IsPrimary
                    });
                }

                module.Topics.Add(topic);
            }

            _db.TrackModules.Add(module);
        }

        await _db.SaveChangesAsync();
        await SeedChecklistDefaultsAsync();
        await SeedChapterExamsDefaultsAsync();
        _logger.LogInformation("Seeded {ModuleCount} modules and {ResourceCount} Google Drive resources into SQL Server.",
            payload.Modules.Count, resourcesByRelPath.Count);
    }

    /// <summary>
    /// Provisions starter per-user checklist items from __TEMPLATE__ rows on a student's first sign-in.
    /// </summary>
    public async Task EnsureUserSeededAsync(string userEmail, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userEmail)) return;
        string norm = userEmail.Trim().ToLowerInvariant();
        if (await _db.ChecklistTasks.AnyAsync(t => t.UserEmail == norm, ct))
            return;

        var templates = await _db.ChecklistTasks
            .AsNoTracking()
            .Where(t => t.UserEmail == ChecklistTask.TemplateOwner)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync(ct);

        if (templates.Count == 0) return;

        foreach (var tpl in templates)
        {
            _db.ChecklistTasks.Add(new ChecklistTask
            {
                UserEmail = norm,
                TrackModuleId = tpl.TrackModuleId,
                Title = tpl.Title,
                Category = tpl.Category,
                SourceHint = tpl.SourceHint,
                IsCompleted = false,
                DisplayOrder = tpl.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedChapterExamsDefaultsAsync()
    {
        var modules = await _db.TrackModules.AsNoTracking().OrderBy(m => m.DisplayOrder).ToListAsync();
        if (modules.Count == 0) return;

        var existingModuleIds = await _db.ChapterExams
            .AsNoTracking()
            .Where(e => e.OwnerEmail == ChapterExam.TemplateOwner && e.TrackModuleId != null)
            .Select(e => e.TrackModuleId!.Value)
            .ToHashSetAsync();

        bool hasMock = await _db.ChapterExams.AnyAsync(e => e.OwnerEmail == ChapterExam.TemplateOwner && e.TrackModuleId == null);
        var exams = new List<ChapterExam>();

        foreach (var m in modules)
        {
            if (m.IsUpcoming || existingModuleIds.Contains(m.Id)) continue;
            exams.Add(new ChapterExam
            {
                OwnerEmail = ChapterExam.TemplateOwner,
                TrackModuleId = m.Id,
                Title = $"Chapter {m.Code} Checkpoint Exam: {m.TitleEn}",
                TitleAr = $"امتحان نهاية شابتر {m.Code}: {m.TitleAr}",
                Description = $"End-of-chapter assessment covering all topics, conventions & quick checks in {m.TitleEn}.",
                TimeLimitMinutes = m.Code is "04" or "06" or "08" ? 20 : 12,
                PassingScorePercent = 70,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!hasMock)
        {
            exams.Add(new ChapterExam
            {
                OwnerEmail = ChapterExam.TemplateOwner,
                TrackModuleId = null,
                Title = "Comprehensive Backend Mock Exam (All Chapters)",
                TitleAr = "امتحان شامل على كامل التراك (C# · OOP · SQL · LINQ · EF · MVC)",
                Description = "Mixed timed mock assessment pulling core & interview-critical questions across all completed chapters.",
                TimeLimitMinutes = 35,
                PassingScorePercent = 75,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (exams.Count > 0)
        {
            _db.ChapterExams.AddRange(exams);
            await _db.SaveChangesAsync();
        }
    }

    private async Task SeedChecklistDefaultsAsync()
    {
        if (await _db.ChecklistTasks.AnyAsync(t => t.UserEmail == ChecklistTask.TemplateOwner)) return;
        if (!await _db.TrackModules.AnyAsync()) return;

        var mods = await _db.TrackModules.AsNoTracking().ToDictionaryAsync(m => m.Code, m => (int?)m.Id);
        int? Mod(string code) => mods.TryGetValue(code, out var id) ? id : null;

        var defaults = new List<ChecklistTask>
        {
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("01"), Title = "حل C# Basics Assignments & Session Exercises (Eng Hossam Mostafa)", Category = "Assignment", SourceHint = "Eng Hossam Mostafa/01 C# Basics", DisplayOrder = 1 },
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("01"), Title = "حل أسئلة Workshops C# Basics (Aya Ali & Menna Elashry)", Category = "Workshop", SourceHint = "Aya Ali/01 C# Basics & Menna Elashry/01 C#Basics", DisplayOrder = 2 },
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("02"), Title = "حل وتطبيق OOP Assignments 01-04 + Explanation Videos", Category = "Assignment", SourceHint = "Assignments Explanation/01 OOP", DisplayOrder = 3 },
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("02"), Title = "تطبيق مشروع City Library Console OOP Design (Menna Elashry)", Category = "Workshop", SourceHint = "Menna Elashry/02 OOP/Session_02_(Project)", DisplayOrder = 4 },
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("02"), Title = "حل امتحان شابتر 02 OOP في قسم Exams بالمنصة", Category = "Exam", SourceHint = "/Exams (Chapter 02 Checkpoint Exam)", DisplayOrder = 5 },
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("03"), Title = "حل Threading Workshop (Aya Ali & Menna Elashry 8 Questions)", Category = "Workshop", SourceHint = "Aya Ali/03 Threading & Menna Elashry/03 ADVC#", DisplayOrder = 6 },
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("04"), Title = "تطبيق ERD & DDL Mapping على Case Studies (Eng Hossam Sessions 01-04)", Category = "Assignment", SourceHint = "Eng Hossam Mostafa/04 Database", DisplayOrder = 7 },
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("04"), Title = "تطبيق DML/Joins/Window/CTE/SP/Triggers/Views على Hospital.bak", Category = "Assignment", SourceHint = "Eng Hossam Mostafa/04 Database/Session 05..09", DisplayOrder = 8 },
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("04"), Title = "اجتياز امتحان Database Checkpoint Exam في قسم Exams بالمنصة", Category = "Exam", SourceHint = "/Exams (Chapter 04 Checkpoint Exam)", DisplayOrder = 9 },
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("04"), Title = "حل تدريبات Database Workshops (Aya Ali Week 01-04 & Menna 01-04)", Category = "Workshop", SourceHint = "Aya Ali/04 Database & Menna Elashry/04 Database", DisplayOrder = 10 },
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("05"), Title = "حل LINQ Assignments 01 & 02 + Workshop Recap Videos", Category = "Assignment", SourceHint = "Eng Hossam Mostafa/05 LINQ & Aya Ali/05 LINQ", DisplayOrder = 11 },
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("06"), Title = "حل EF Core Sessions & Assignments 01-04 (Eng Hossam Mostafa)", Category = "Assignment", SourceHint = "Eng Hossam Mostafa/06 Entity Framework Core", DisplayOrder = 12 },
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("06"), Title = "تطبيق مشروع Library Management System بـ EF Core (Aya Ali & Menna)", Category = "Workshop", SourceHint = "Aya Ali/06 Entity Framework Core & Menna Elashry/06 EFCore", DisplayOrder = 13 },
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("08"), Title = "بناء مشروع Gym Management System بـ ASP.NET Core MVC (Sessions 01-04)", Category = "Workshop", SourceHint = "Eng Hossam Mostafa/08 ASP.NET Core MVC", DisplayOrder = 14 },
            new() { UserEmail = ChecklistTask.TemplateOwner, TrackModuleId = Mod("09"), Title = "مراجعة نهائية عبر Architecture Mind-Map + Convention Keys + Mock Exam", Category = "Interview Prep", SourceHint = "/Architecture & /Topics/Conventions", DisplayOrder = 15 },
        };

        _db.ChecklistTasks.AddRange(defaults);
        await _db.SaveChangesAsync();
    }

    private async Task<bool> NpgsqlTableExistsAsync(string tableName)
    {
        var conn = _db.Database.GetDbConnection();
        bool shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose) await conn.OpenAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = @t)";
            var param = cmd.CreateParameter();
            param.ParameterName = "@t";
            param.Value = tableName;
            cmd.Parameters.Add(param);
            var scalar = await cmd.ExecuteScalarAsync();
            return scalar is bool b && b;
        }
        finally
        {
            if (shouldClose) await conn.CloseAsync();
        }
    }

    private static string Trunc(string? value, int maxLen)
        => string.IsNullOrEmpty(value) ? string.Empty : (value.Length <= maxLen ? value : value[..maxLen]);
}
