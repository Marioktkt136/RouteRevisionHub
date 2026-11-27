using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteRevisionHub.Data;
using RouteRevisionHub.Models.Entities;
using RouteRevisionHub.Services;

namespace RouteRevisionHub.Controllers;

[Authorize(Policy = "DriveGroupMember")]
[Route("api")]
public class ApiController : Controller
{
    private readonly AppDbContext _db;
    private readonly RevisionSearchService _search;
    private readonly CourseSyncService _sync;
    private readonly GoogleDriveService _drive;
    private readonly ICurrentUserAccessor _currentUser;

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "NotStarted", "InProgress", "NeedsReview", "Mastered"
    };

    public ApiController(
        AppDbContext db,
        RevisionSearchService search,
        CourseSyncService sync,
        GoogleDriveService drive,
        ICurrentUserAccessor currentUser)
    {
        _db = db;
        _search = search;
        _sync = sync;
        _drive = drive;
        _currentUser = currentUser;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(string? q, int? moduleId, string? type, string? instructor)
    {
        var result = await _search.SearchAsync(q, moduleId, type, instructor);
        return Ok(result);
    }

    // ---------- Google Drive Per-User Proxy Streaming & Preview ----------

    [HttpGet("media/stream/{id:int}")]
    public async Task<IActionResult> StreamMedia(int id, [FromQuery] bool download = false, CancellationToken ct = default)
    {
        var res = await _db.CourseResources.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (res is null) return NotFound("Resource record not found.");
        if (string.IsNullOrWhiteSpace(res.DriveFileId))
            return NotFound("This resource does not have a mapped Google Drive File ID.");

        string contentType = ResolveContentType(res.Extension.ToLowerInvariant());
        var proxyResult = await _drive.ProxyStreamAsync(HttpContext, res.DriveFileId, res.FileName, contentType, download, ct);
        if (proxyResult.Streamed)
            return new EmptyResult();

        return StatusCode(proxyResult.StatusCode, new { error = proxyResult.ErrorMessage });
    }

    [HttpGet("media/download/{id:int}")]
    public async Task<IActionResult> DownloadMedia(int id, CancellationToken ct = default)
        => await StreamMedia(id, download: true, ct);

    [HttpGet("media/preview/{id:int}")]
    public async Task<IActionResult> PreviewMedia(int id, CancellationToken ct = default)
    {
        var res = await _db.CourseResources
            .AsNoTracking()
            .Include(r => r.TopicRefs).ThenInclude(s => s.Topic)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (res is null) return NotFound();

        string ext = res.Extension.ToLowerInvariant();
        string? liveCodeText = null;
        if (IsTextCodeExt(ext) && !string.IsNullOrWhiteSpace(res.DriveFileId) && res.SizeMb <= 0.5m)
        {
            string? token = await _drive.GetValidUserAccessTokenAsync(HttpContext, ct);
            if (!string.IsNullOrWhiteSpace(token))
            {
                var bytes = await _drive.DownloadSmallFileBytesAsync(token, res.DriveFileId, 512 * 1024, ct);
                if (bytes is not null)
                    liveCodeText = Encoding.UTF8.GetString(bytes);
            }
        }

        string? drivePreviewUrl = string.IsNullOrWhiteSpace(res.DriveFileId)
            ? null
            : $"https://drive.google.com/file/d/{res.DriveFileId}/preview";
        string? driveViewUrl = res.DriveWebViewLink
            ?? (string.IsNullOrWhiteSpace(res.DriveFileId) ? null : $"https://drive.google.com/file/d/{res.DriveFileId}/view");
        string? driveFolderUrl = string.IsNullOrWhiteSpace(res.DriveParentFolderId)
            ? null
            : $"https://drive.google.com/drive/folders/{res.DriveParentFolderId}";

        return Ok(new
        {
            res.Id,
            res.FileName,
            res.RelativePath,
            FullPath = driveViewUrl ?? res.FullPath,
            res.ResourceType,
            res.Extension,
            res.Instructor,
            res.SessionName,
            res.DurationFormatted,
            res.DurationSeconds,
            res.PageOrSlideCount,
            res.SizeMb,
            StructuredPreviewJson = res.StructuredPreviewJson,
            RawText = liveCodeText ?? res.SearchableText,
            CanStreamInBrowser = CanRenderInlineInTab(ext),
            CanPlayVideo = IsMediaExt(ext),
            IsImage = IsImageExt(ext),
            StreamUrl = $"/api/media/stream/{res.Id}",
            DownloadUrl = $"/api/media/download/{res.Id}",
            res.DriveFileId,
            res.DriveParentFolderId,
            DriveWebViewLink = driveViewUrl,
            DrivePreviewUrl = drivePreviewUrl,
            DriveFolderUrl = driveFolderUrl,
            LinkedTopics = res.TopicRefs
                .OrderByDescending(x => x.RelevanceScore)
                .Select(x => new { x.Topic.Slug, x.Topic.TitleEn, x.MatchedLocations, x.MatchReason })
                .ToList()
        });
    }

    [HttpPost("media/open/{id:int}")]
    public async Task<IActionResult> OpenExternal(int id)
    {
        var res = await _db.CourseResources.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (res is null) return NotFound();

        string ext = res.Extension.ToLowerInvariant();
        string? driveViewUrl = res.DriveWebViewLink
            ?? (string.IsNullOrWhiteSpace(res.DriveFileId) ? null : $"https://drive.google.com/file/d/{res.DriveFileId}/view");
        string? drivePreviewUrl = string.IsNullOrWhiteSpace(res.DriveFileId)
            ? null
            : $"https://drive.google.com/file/d/{res.DriveFileId}/preview";
        string? driveFolderUrl = string.IsNullOrWhiteSpace(res.DriveParentFolderId)
            ? null
            : $"https://drive.google.com/drive/folders/{res.DriveParentFolderId}";

        return Ok(new
        {
            opened = false,
            launchedNatively = false,
            reason = "cloud-drive",
            desktop = "Google Drive Online",
            resourceId = res.Id,
            file = res.FileName,
            extension = ext,
            resourceType = res.ResourceType,
            isMedia = IsMediaExt(ext),
            canRenderInTab = CanRenderInlineInTab(ext),
            fullPath = driveViewUrl ?? res.RelativePath,
            folderPath = driveFolderUrl ?? string.Empty,
            streamUrl = $"/api/media/stream/{res.Id}",
            downloadUrl = $"/api/media/download/{res.Id}",
            driveWebViewLink = driveViewUrl,
            drivePreviewUrl,
            driveFolderUrl
        });
    }

    [HttpPost("media/reveal/{id:int}")]
    public async Task<IActionResult> RevealInExplorer(int id)
    {
        var res = await _db.CourseResources.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (res is null) return NotFound();

        string? driveFolderUrl = string.IsNullOrWhiteSpace(res.DriveParentFolderId)
            ? null
            : $"https://drive.google.com/drive/folders/{res.DriveParentFolderId}";

        return Ok(new
        {
            revealed = false,
            launchedNatively = false,
            reason = "cloud-drive",
            desktop = "Google Drive Online",
            resourceId = res.Id,
            file = res.FileName,
            fullPath = res.DriveWebViewLink ?? res.FullPath,
            folderPath = driveFolderUrl ?? string.Empty,
            driveFolderUrl
        });
    }

    [HttpGet("media/folder/{id:int}")]
    public async Task<IActionResult> GetResourceFolder(int id)
    {
        var res = await _db.CourseResources.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (res is null) return NotFound();

        string relFolder = Path.GetDirectoryName(res.RelativePath)?.Replace('\\', '/') ?? res.ModuleFolder;
        var siblings = await _db.CourseResources
            .AsNoTracking()
            .Where(r => !string.IsNullOrEmpty(res.DriveParentFolderId)
                ? r.DriveParentFolderId == res.DriveParentFolderId
                : r.RelativePath.StartsWith(relFolder + "/"))
            .OrderBy(r => r.FileName)
            .ToListAsync();

        var files = siblings.Select(s =>
        {
            string ext = s.Extension.ToLowerInvariant();
            return new
            {
                resourceId = (int?)s.Id,
                fileName = s.FileName,
                extension = ext,
                resourceType = s.ResourceType,
                durationFormatted = s.DurationFormatted,
                sizeMb = s.SizeMb,
                isSelected = s.Id == res.Id,
                isMedia = IsMediaExt(ext),
                canRenderInTab = CanRenderInlineInTab(ext),
                streamUrl = $"/api/media/stream/{s.Id}",
                downloadUrl = $"/api/media/download/{s.Id}",
                driveWebViewLink = s.DriveWebViewLink
            };
        })
        .OrderByDescending(x => x.isSelected)
        .ThenBy(x => x.fileName, StringComparer.OrdinalIgnoreCase)
        .ToList();

        string driveFolderUrl = string.IsNullOrWhiteSpace(res.DriveParentFolderId)
            ? "https://drive.google.com"
            : $"https://drive.google.com/drive/folders/{res.DriveParentFolderId}";

        return Ok(new
        {
            resourceId = res.Id,
            selectedFile = res.FileName,
            fullPath = res.DriveWebViewLink ?? res.FullPath,
            folderPath = driveFolderUrl,
            driveFolderUrl,
            relativeFolder = relFolder,
            desktop = "Google Drive Online",
            files,
            subfolders = Array.Empty<object>()
        });
    }

    // ---------- Per-User Topic Progress & Quiz Attempts ----------

    public record ProgressUpdateDto(int TopicId, string Status, int ConfidenceLevel, string? UserNotes, bool IncrementReview);

    [HttpPost("progress")]
    public async Task<IActionResult> UpdateProgress([FromBody] ProgressUpdateDto dto)
    {
        var topicExists = await _db.Topics.AnyAsync(t => t.Id == dto.TopicId);
        if (!topicExists) return NotFound();

        string userEmail = _currentUser.Email;
        string safeStatus = ValidStatuses.Contains(dto.Status) ? dto.Status : "InProgress";
        int safeConfidence = Math.Clamp(dto.ConfidenceLevel, 1, 5);

        var prog = await _db.TopicProgresses.FirstOrDefaultAsync(p => p.UserEmail == userEmail && p.TopicId == dto.TopicId);
        if (prog is null)
        {
            prog = new TopicProgress { UserEmail = userEmail, TopicId = dto.TopicId };
            _db.TopicProgresses.Add(prog);
        }

        prog.Status = safeStatus;
        prog.ConfidenceLevel = safeConfidence;
        if (dto.UserNotes is not null)
            prog.UserNotes = dto.UserNotes;
        if (dto.IncrementReview)
        {
            prog.ReviewCount += 1;
            prog.LastReviewedAt = DateTime.UtcNow;
        }
        if (safeStatus == "Mastered")
        {
            prog.VideoWatched = true;
            prog.SlidesReviewed = true;
            prog.PracticeDone = true;
        }
        else if (safeStatus == "NotStarted")
        {
            prog.VideoWatched = false;
            prog.SlidesReviewed = false;
            prog.PracticeDone = false;
        }
        prog.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new
        {
            prog.TopicId,
            prog.Status,
            prog.ConfidenceLevel,
            prog.UserNotes,
            prog.ReviewCount,
            prog.LastReviewedAt
        });
    }

    public record QuizAnswerDto(int QuestionId, string SelectedAnswer);

    [HttpPost("quiz/answer")]
    public async Task<IActionResult> RecordQuizAnswer([FromBody] QuizAnswerDto dto)
    {
        var question = await _db.FlashcardQuestions.AsNoTracking().FirstOrDefaultAsync(q => q.Id == dto.QuestionId);
        if (question is null) return NotFound();

        string userEmail = _currentUser.Email;
        bool isCorrect = string.Equals(
            (dto.SelectedAnswer ?? string.Empty).Trim(),
            question.CorrectAnswer.Trim(),
            StringComparison.OrdinalIgnoreCase);

        _db.QuizAttempts.Add(new QuizAttempt
        {
            UserEmail = userEmail,
            FlashcardQuestionId = question.Id,
            TopicId = question.TopicId,
            SelectedAnswer = (dto.SelectedAnswer ?? string.Empty).Trim(),
            IsCorrect = isCorrect,
            AttemptedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            isCorrect,
            correctAnswer = question.CorrectAnswer,
            explanation = question.Explanation,
            sourceCitation = question.SourceCitation
        });
    }

    // ---------- Open-for-Expansion Endpoints (Live Google Drive Scan & Shared Knowledge CRUD) ----------

    public record ScanRequestDto(bool AutoCreateTopics = true);

    [HttpPost("sync/scan")]
    public async Task<IActionResult> ScanCourseFolder([FromBody] ScanRequestDto? dto, CancellationToken ct)
    {
        var report = await _sync.ScanAndSyncAsync(dto?.AutoCreateTopics ?? true, ct);
        return Ok(report);
    }

    public record CreateModuleDto(string Code, string TitleEn, string? TitleAr, string? Description, string? BadgeColor);

    [HttpPost("modules")]
    public async Task<IActionResult> CreateModule([FromBody] CreateModuleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.TitleEn))
            return BadRequest("Module Code and TitleEn are required.");

        string code = dto.Code.Trim();
        if (await _db.TrackModules.AnyAsync(m => m.Code == code))
            return Conflict($"Module code '{code}' already exists.");

        int nextOrder = (await _db.TrackModules.MaxAsync(m => (int?)m.DisplayOrder) ?? 0) + 1;
        var module = new TrackModule
        {
            Code = code.Length > 24 ? code[..24] : code,
            TitleEn = dto.TitleEn.Trim(),
            TitleAr = string.IsNullOrWhiteSpace(dto.TitleAr) ? dto.TitleEn.Trim() : dto.TitleAr.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            DisplayOrder = nextOrder,
            BadgeColor = string.IsNullOrWhiteSpace(dto.BadgeColor) ? "#38bdf8" : dto.BadgeColor.Trim(),
            IsUpcoming = false
        };
        _db.TrackModules.Add(module);
        await _db.SaveChangesAsync();

        return Ok(new { module.Id, module.Code, module.TitleEn, module.TitleAr });
    }

    public record CreateTopicDto(
        int TrackModuleId,
        string TitleEn,
        string? TitleAr,
        string? SessionLabel,
        string? Priority,
        string? CodeLanguage,
        string? SummaryAr,
        string? SummaryEn,
        string? CodeSnippet,
        string? CommonMistakes,
        string? InterviewQuestions,
        string? RawConventionKeys);

    [HttpPost("topics")]
    public async Task<IActionResult> CreateTopic([FromBody] CreateTopicDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TitleEn))
            return BadRequest("TitleEn is required.");

        var mod = await _db.TrackModules.FirstOrDefaultAsync(m => m.Id == dto.TrackModuleId);
        if (mod is null) return NotFound("Target TrackModule not found.");

        string baseSlug = CourseSyncService.Slugify(dto.TitleEn);
        string slug = baseSlug;
        int n = 2;
        while (await _db.Topics.AnyAsync(t => t.Slug == slug)) slug = $"{baseSlug}-{n++}";

        int nextOrder = (await _db.Topics.Where(t => t.TrackModuleId == mod.Id).MaxAsync(t => (int?)t.DisplayOrder) ?? 0) + 1;
        var topic = new Topic
        {
            TrackModuleId = mod.Id,
            Slug = slug,
            TitleEn = dto.TitleEn.Trim(),
            TitleAr = string.IsNullOrWhiteSpace(dto.TitleAr) ? dto.TitleEn.Trim() : dto.TitleAr.Trim(),
            SessionLabel = string.IsNullOrWhiteSpace(dto.SessionLabel) ? "Custom Topic" : dto.SessionLabel.Trim(),
            DisplayOrder = nextOrder,
            Priority = string.IsNullOrWhiteSpace(dto.Priority) ? "Core" : dto.Priority.Trim(),
            CodeLanguage = string.IsNullOrWhiteSpace(dto.CodeLanguage) ? "csharp" : dto.CodeLanguage.Trim(),
            SummaryAr = dto.SummaryAr?.Trim() ?? string.Empty,
            SummaryEn = dto.SummaryEn?.Trim() ?? string.Empty,
            CodeSnippet = string.IsNullOrWhiteSpace(dto.CodeSnippet) ? null : dto.CodeSnippet,
            CommonMistakes = dto.CommonMistakes?.Trim() ?? string.Empty,
            InterviewQuestions = dto.InterviewQuestions?.Trim() ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(dto.RawConventionKeys))
        {
            foreach (var rawLine in dto.RawConventionKeys.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = rawLine.Split('|').Select(p => p.Trim()).ToArray();
                if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0])) continue;
                topic.ConventionKeys.Add(new ConventionKey
                {
                    KeyName = parts[0],
                    Category = parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) ? parts[1] : "Syntax",
                    ShortRule = parts.Length > 2 && !string.IsNullOrEmpty(parts[2]) ? parts[2] : parts[0],
                    ExampleSnippet = parts.Length > 3 && !string.IsNullOrEmpty(parts[3]) ? parts[3] : null,
                    IsCritical = true
                });
            }
        }

        if (mod.IsUpcoming) mod.IsUpcoming = false;
        _db.Topics.Add(topic);
        await _db.SaveChangesAsync();

        return Ok(new { topic.Id, topic.Slug, topic.TitleEn });
    }

    public record UpdateTopicDto(
        string TitleEn,
        string TitleAr,
        string SessionLabel,
        string Priority,
        string CodeLanguage,
        string SummaryAr,
        string SummaryEn,
        string? CodeSnippet,
        string CommonMistakes,
        string InterviewQuestions);

    [HttpPut("topics/{id:int}")]
    public async Task<IActionResult> UpdateTopic(int id, [FromBody] UpdateTopicDto dto)
    {
        var topic = await _db.Topics.FirstOrDefaultAsync(t => t.Id == id);
        if (topic is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.TitleEn)) topic.TitleEn = dto.TitleEn.Trim();
        if (!string.IsNullOrWhiteSpace(dto.TitleAr)) topic.TitleAr = dto.TitleAr.Trim();
        topic.SessionLabel = dto.SessionLabel?.Trim() ?? topic.SessionLabel;
        topic.Priority = dto.Priority?.Trim() ?? topic.Priority;
        topic.CodeLanguage = dto.CodeLanguage?.Trim() ?? topic.CodeLanguage;
        topic.SummaryAr = dto.SummaryAr ?? string.Empty;
        topic.SummaryEn = dto.SummaryEn ?? string.Empty;
        topic.CodeSnippet = string.IsNullOrWhiteSpace(dto.CodeSnippet) ? null : dto.CodeSnippet;
        topic.CommonMistakes = dto.CommonMistakes ?? string.Empty;
        topic.InterviewQuestions = dto.InterviewQuestions ?? string.Empty;

        await _db.SaveChangesAsync();
        return Ok(new { topic.Id, topic.Slug });
    }

    public record AddConventionKeyDto(string KeyName, string Category, string ShortRule, string? ExampleSnippet, bool IsCritical);

    [HttpPost("topics/{topicId:int}/conventions")]
    public async Task<IActionResult> AddConventionKey(int topicId, [FromBody] AddConventionKeyDto dto)
    {
        if (!await _db.Topics.AnyAsync(t => t.Id == topicId)) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.KeyName) || string.IsNullOrWhiteSpace(dto.ShortRule))
            return BadRequest("KeyName and ShortRule are required.");

        var key = new ConventionKey
        {
            TopicId = topicId,
            KeyName = dto.KeyName.Trim(),
            Category = string.IsNullOrWhiteSpace(dto.Category) ? "Syntax" : dto.Category.Trim(),
            ShortRule = dto.ShortRule.Trim(),
            ExampleSnippet = string.IsNullOrWhiteSpace(dto.ExampleSnippet) ? null : dto.ExampleSnippet.Trim(),
            IsCritical = dto.IsCritical
        };
        _db.ConventionKeys.Add(key);
        await _db.SaveChangesAsync();
        return Ok(new { key.Id, key.KeyName });
    }

    [HttpDelete("conventions/{id:int}")]
    public async Task<IActionResult> DeleteConventionKey(int id)
    {
        var key = await _db.ConventionKeys.FirstOrDefaultAsync(k => k.Id == id);
        if (key is null) return NotFound();
        _db.ConventionKeys.Remove(key);
        await _db.SaveChangesAsync();
        return Ok(new { deleted = true });
    }

    public record AddFlashcardDto(string QuestionType, string Prompt, string? CodeBlock, string? RawOptions, string CorrectAnswer, string? Explanation, string? SourceCitation);

    [HttpPost("topics/{topicId:int}/flashcards")]
    public async Task<IActionResult> AddFlashcard(int topicId, [FromBody] AddFlashcardDto dto)
    {
        if (!await _db.Topics.AnyAsync(t => t.Id == topicId)) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Prompt) || string.IsNullOrWhiteSpace(dto.CorrectAnswer))
            return BadRequest("Prompt and CorrectAnswer are required.");

        var options = (dto.RawOptions ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim())
            .Where(o => o.Length > 0)
            .ToList();

        var card = new FlashcardQuestion
        {
            TopicId = topicId,
            QuestionType = string.IsNullOrWhiteSpace(dto.QuestionType) ? "MCQ" : dto.QuestionType.Trim(),
            Prompt = dto.Prompt.Trim(),
            CodeBlock = string.IsNullOrWhiteSpace(dto.CodeBlock) ? null : dto.CodeBlock,
            OptionsJson = options.Count > 0 ? JsonSerializer.Serialize(options) : null,
            CorrectAnswer = dto.CorrectAnswer.Trim(),
            Explanation = dto.Explanation?.Trim() ?? string.Empty,
            SourceCitation = dto.SourceCitation?.Trim() ?? "Custom Added Note"
        };
        _db.FlashcardQuestions.Add(card);
        await _db.SaveChangesAsync();
        return Ok(new { card.Id });
    }

    [HttpGet("resources/search")]
    public async Task<IActionResult> SearchResourcesForCitation(string? q)
    {
        var query = _db.CourseResources.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            string like = $"%{q.Trim()}%";
            bool isPg = _db.Database.IsNpgsql();
            query = isPg
                ? query.Where(r => EF.Functions.ILike(r.FileName, like) || EF.Functions.ILike(r.RelativePath, like))
                : query.Where(r => EF.Functions.Like(r.FileName, like) || EF.Functions.Like(r.RelativePath, like));
        }

        var items = await query
            .OrderByDescending(r => r.Id)
            .Take(25)
            .Select(r => new { r.Id, r.FileName, r.RelativePath, r.ResourceType, r.DurationFormatted })
            .ToListAsync();

        return Ok(items);
    }

    public record AttachCitationDto(int CourseResourceId, string? MatchReason, string? MatchedLocations, int? FirstTimestampSeconds, bool IsPrimary);

    [HttpPost("topics/{topicId:int}/citations")]
    public async Task<IActionResult> AttachCitationToTopic(int topicId, [FromBody] AttachCitationDto dto)
    {
        var topic = await _db.Topics.Include(t => t.TrackModule).FirstOrDefaultAsync(t => t.Id == topicId);
        if (topic is null) return NotFound("Topic not found.");
        var res = await _db.CourseResources.AsNoTracking().FirstOrDefaultAsync(r => r.Id == dto.CourseResourceId);
        if (res is null) return NotFound("Resource not found.");

        var existing = await _db.TopicSourceRefs.FirstOrDefaultAsync(x => x.TopicId == topicId && x.CourseResourceId == dto.CourseResourceId);
        if (existing is not null)
        {
            existing.MatchReason = string.IsNullOrWhiteSpace(dto.MatchReason) ? existing.MatchReason : dto.MatchReason.Trim();
            existing.MatchedLocations = string.IsNullOrWhiteSpace(dto.MatchedLocations) ? existing.MatchedLocations : dto.MatchedLocations.Trim();
            if (dto.FirstTimestampSeconds.HasValue) existing.FirstTimestampSeconds = dto.FirstTimestampSeconds;
            existing.IsPrimary = dto.IsPrimary || existing.IsPrimary;
            await _db.SaveChangesAsync();
            return Ok(new { existing.Id, updated = true });
        }

        var link = new TopicSourceRef
        {
            TopicId = topicId,
            CourseResourceId = dto.CourseResourceId,
            RelevanceScore = dto.IsPrimary ? 95 : 80,
            MatchReason = string.IsNullOrWhiteSpace(dto.MatchReason) ? "Manually linked to topic" : dto.MatchReason.Trim(),
            MatchedLocations = string.IsNullOrWhiteSpace(dto.MatchedLocations) ? res.SessionName : dto.MatchedLocations.Trim(),
            FirstTimestampSeconds = dto.FirstTimestampSeconds,
            MatchedExcerpt = res.RelativePath,
            IsPrimary = dto.IsPrimary
        };
        _db.TopicSourceRefs.Add(link);
        if (topic.TrackModule.IsUpcoming) topic.TrackModule.IsUpcoming = false;
        await _db.SaveChangesAsync();

        return Ok(new { link.Id, created = true });
    }

    [HttpDelete("topics/{topicId:int}/citations/{refId:int}")]
    public async Task<IActionResult> DeleteTopicCitation(int topicId, int refId)
    {
        var link = await _db.TopicSourceRefs.FirstOrDefaultAsync(x => x.Id == refId && x.TopicId == topicId);
        if (link is null) return NotFound();
        _db.TopicSourceRefs.Remove(link);
        await _db.SaveChangesAsync();
        return Ok(new { deleted = true });
    }

    // ---------- Per-User Checklist Endpoints ----------

    public record ChecklistStepDto(int TopicId, string Step, bool Checked);

    [HttpPost("checklist/topic-step")]
    public async Task<IActionResult> ToggleChecklistStep([FromBody] ChecklistStepDto dto)
    {
        var topic = await _db.Topics.FirstOrDefaultAsync(t => t.Id == dto.TopicId);
        if (topic is null) return NotFound();

        string userEmail = _currentUser.Email;
        var prog = await _db.TopicProgresses.FirstOrDefaultAsync(p => p.UserEmail == userEmail && p.TopicId == topic.Id);
        if (prog is null)
        {
            prog = new TopicProgress { UserEmail = userEmail, TopicId = topic.Id };
            _db.TopicProgresses.Add(prog);
        }

        switch ((dto.Step ?? string.Empty).ToLowerInvariant())
        {
            case "video": prog.VideoWatched = dto.Checked; break;
            case "slides": prog.SlidesReviewed = dto.Checked; break;
            case "practice": prog.PracticeDone = dto.Checked; break;
            case "all":
                prog.VideoWatched = dto.Checked;
                prog.SlidesReviewed = dto.Checked;
                prog.PracticeDone = dto.Checked;
                break;
            default:
                return BadRequest("Step must be video, slides, practice, or all.");
        }

        int doneCount = (prog.VideoWatched ? 1 : 0) + (prog.SlidesReviewed ? 1 : 0) + (prog.PracticeDone ? 1 : 0);
        if (doneCount == 3)
        {
            prog.Status = "Mastered";
            prog.ConfidenceLevel = Math.Max(prog.ConfidenceLevel, 4);
            prog.LastReviewedAt = DateTime.UtcNow;
            if (prog.ReviewCount == 0) prog.ReviewCount = 1;
        }
        else if (doneCount > 0)
        {
            if (prog.Status is "NotStarted" or "Mastered")
                prog.Status = "InProgress";
        }
        else
        {
            prog.Status = "NotStarted";
        }
        prog.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        var summary = await BuildChecklistSummaryAsync(userEmail, topic.TrackModuleId);
        return Ok(new
        {
            topicId = topic.Id,
            videoWatched = prog.VideoWatched,
            slidesReviewed = prog.SlidesReviewed,
            practiceDone = prog.PracticeDone,
            status = prog.Status,
            summary
        });
    }

    public record CreateChecklistTaskDto(int? TrackModuleId, string Title, string? Category, string? SourceHint);

    [HttpPost("checklist/custom")]
    public async Task<IActionResult> CreateChecklistTask([FromBody] CreateChecklistTaskDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");

        string userEmail = _currentUser.Email;
        int nextOrder = (await _db.ChecklistTasks.Where(t => t.UserEmail == userEmail).MaxAsync(t => (int?)t.DisplayOrder) ?? 0) + 1;
        var item = new ChecklistTask
        {
            UserEmail = userEmail,
            TrackModuleId = dto.TrackModuleId is > 0 ? dto.TrackModuleId : null,
            Title = dto.Title.Trim().Length > 240 ? dto.Title.Trim()[..240] : dto.Title.Trim(),
            Category = string.IsNullOrWhiteSpace(dto.Category) ? "Custom" : dto.Category.Trim(),
            SourceHint = string.IsNullOrWhiteSpace(dto.SourceHint) ? null : dto.SourceHint.Trim(),
            DisplayOrder = nextOrder,
            CreatedAt = DateTime.UtcNow
        };
        _db.ChecklistTasks.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { item.Id, item.Title, item.Category, item.SourceHint });
    }

    [HttpPost("checklist/custom/{id:int}/toggle")]
    public async Task<IActionResult> ToggleChecklistTask(int id)
    {
        string userEmail = _currentUser.Email;
        var item = await _db.ChecklistTasks.FirstOrDefaultAsync(x => x.Id == id && x.UserEmail == userEmail);
        if (item is null) return NotFound();

        item.IsCompleted = !item.IsCompleted;
        item.CompletedAt = item.IsCompleted ? DateTime.UtcNow : null;
        await _db.SaveChangesAsync();

        var summary = await BuildChecklistSummaryAsync(userEmail, item.TrackModuleId);
        return Ok(new { item.Id, item.IsCompleted, summary });
    }

    [HttpDelete("checklist/custom/{id:int}")]
    public async Task<IActionResult> DeleteChecklistTask(int id)
    {
        string userEmail = _currentUser.Email;
        var item = await _db.ChecklistTasks.FirstOrDefaultAsync(x => x.Id == id && x.UserEmail == userEmail);
        if (item is null) return NotFound();
        _db.ChecklistTasks.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { deleted = true });
    }

    private async Task<object> BuildChecklistSummaryAsync(string userEmail, int? focusModuleId = null)
    {
        var activeTopics = await _db.Topics.AsNoTracking()
            .Include(t => t.TrackModule)
            .Include(t => t.Progresses.Where(p => p.UserEmail == userEmail))
            .Where(t => !t.TrackModule.IsUpcoming)
            .ToListAsync();

        int totalTopics = activeTopics.Count;
        int masteredTopics = activeTopics.Count(t => t.Progress?.Status == "Mastered");
        int totalSteps = totalTopics * 3;
        int completedSteps = activeTopics.Sum(t =>
            (t.Progress?.VideoWatched == true ? 1 : 0) +
            (t.Progress?.SlidesReviewed == true ? 1 : 0) +
            (t.Progress?.PracticeDone == true ? 1 : 0));

        int totalTasks = await _db.ChecklistTasks.CountAsync(x => x.UserEmail == userEmail);
        int completedTasks = await _db.ChecklistTasks.CountAsync(x => x.UserEmail == userEmail && x.IsCompleted);
        int overallPct = (totalSteps + totalTasks) == 0 ? 0
            : (int)Math.Round((completedSteps + completedTasks) * 100.0 / (totalSteps + totalTasks));

        int modPct = 0;
        int modMastered = 0;
        int modTotal = 0;
        if (focusModuleId.HasValue)
        {
            var modTopics = await _db.Topics.AsNoTracking()
                .Include(t => t.Progresses.Where(p => p.UserEmail == userEmail))
                .Where(t => t.TrackModuleId == focusModuleId.Value)
                .ToListAsync();
            modTotal = modTopics.Count;
            modMastered = modTopics.Count(t => t.Progress?.Status == "Mastered");
            int modSteps = modTopics.Sum(t =>
                (t.Progress?.VideoWatched == true ? 1 : 0) +
                (t.Progress?.SlidesReviewed == true ? 1 : 0) +
                (t.Progress?.PracticeDone == true ? 1 : 0));
            modPct = modTotal == 0 ? 0 : (int)Math.Round(modSteps * 100.0 / (modTotal * 3));
        }

        return new
        {
            totalTopics,
            masteredTopics,
            totalSteps,
            completedSteps,
            totalTasks,
            completedTasks,
            overallPercent = overallPct,
            moduleId = focusModuleId,
            modulePercent = modPct,
            moduleMastered = modMastered,
            moduleTotal = modTotal
        };
    }

    // ---------- Per-User Chapter Exams & Deadlines Endpoints ----------

    public record CreateExamDto(int? TrackModuleId, string Title, string? TitleAr, string? Description, int TimeLimitMinutes, int PassingScorePercent, DateTime? DeadlineAt);

    [HttpPost("exams")]
    public async Task<IActionResult> CreateExam([FromBody] CreateExamDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");

        string userEmail = _currentUser.Email;
        var exam = new ChapterExam
        {
            OwnerEmail = userEmail,
            TrackModuleId = dto.TrackModuleId is > 0 ? dto.TrackModuleId : null,
            Title = dto.Title.Trim().Length > 200 ? dto.Title.Trim()[..200] : dto.Title.Trim(),
            TitleAr = string.IsNullOrWhiteSpace(dto.TitleAr) ? dto.Title.Trim() : dto.TitleAr.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            TimeLimitMinutes = Math.Clamp(dto.TimeLimitMinutes <= 0 ? 15 : dto.TimeLimitMinutes, 1, 180),
            PassingScorePercent = Math.Clamp(dto.PassingScorePercent <= 0 ? 70 : dto.PassingScorePercent, 10, 100),
            CreatedAt = DateTime.UtcNow
        };
        _db.ChapterExams.Add(exam);
        await _db.SaveChangesAsync();

        DateTime? deadlineUtc = NormalizeUtcDate(dto.DeadlineAt);
        if (deadlineUtc.HasValue)
        {
            _db.UserExamStates.Add(new UserExamState
            {
                UserEmail = userEmail,
                ChapterExamId = exam.Id,
                DeadlineAt = deadlineUtc,
                UpdatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        return Ok(new { exam.Id, exam.Title, deadlineAt = deadlineUtc });
    }

    public record UpdateExamScheduleDto(DateTime? DeadlineAt, int TimeLimitMinutes, int PassingScorePercent);

    [HttpPatch("exams/{id:int}/schedule")]
    public async Task<IActionResult> UpdateExamSchedule(int id, [FromBody] UpdateExamScheduleDto dto)
    {
        string userEmail = _currentUser.Email;
        var exam = await _db.ChapterExams.FirstOrDefaultAsync(e =>
            e.Id == id && (e.OwnerEmail == ChapterExam.TemplateOwner || e.OwnerEmail == userEmail));
        if (exam is null) return NotFound();

        if (exam.OwnerEmail == userEmail)
        {
            if (dto.TimeLimitMinutes > 0)
                exam.TimeLimitMinutes = Math.Clamp(dto.TimeLimitMinutes, 1, 180);
            if (dto.PassingScorePercent > 0)
                exam.PassingScorePercent = Math.Clamp(dto.PassingScorePercent, 10, 100);
        }

        var state = await _db.UserExamStates.FirstOrDefaultAsync(s => s.UserEmail == userEmail && s.ChapterExamId == id);
        if (state is null)
        {
            state = new UserExamState { UserEmail = userEmail, ChapterExamId = id };
            _db.UserExamStates.Add(state);
        }
        state.DeadlineAt = NormalizeUtcDate(dto.DeadlineAt);
        state.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { exam.Id, state.DeadlineAt, exam.TimeLimitMinutes, exam.PassingScorePercent });
    }

    public record AutoScheduleDto(int DaysPerChapter = 3, DateTime? StartDate = null);

    [HttpPost("exams/auto-schedule")]
    public async Task<IActionResult> AutoScheduleDeadlines([FromBody] AutoScheduleDto dto)
    {
        string userEmail = _currentUser.Email;
        int stepDays = Math.Clamp(dto.DaysPerChapter, 1, 30);
        DateTime cursor = NormalizeUtcDate(dto.StartDate) ?? DateTime.UtcNow.Date;

        var visibleExams = await _db.ChapterExams
            .AsNoTracking()
            .Include(e => e.TrackModule)
            .Where(e => e.OwnerEmail == ChapterExam.TemplateOwner || e.OwnerEmail == userEmail)
            .OrderBy(e => e.TrackModule != null ? e.TrackModule.DisplayOrder : 999)
            .ThenBy(e => e.Id)
            .ToListAsync();

        var existingStates = await _db.UserExamStates
            .Where(s => s.UserEmail == userEmail)
            .ToDictionaryAsync(s => s.ChapterExamId);

        int scheduledCount = 0;
        foreach (var ex in visibleExams)
        {
            if (existingStates.TryGetValue(ex.Id, out var st) && st.IsCompleted)
                continue;

            cursor = cursor.AddDays(stepDays);
            if (st is null)
            {
                st = new UserExamState { UserEmail = userEmail, ChapterExamId = ex.Id };
                _db.UserExamStates.Add(st);
            }
            st.DeadlineAt = cursor;
            st.UpdatedAt = DateTime.UtcNow;
            scheduledCount++;
        }
        await _db.SaveChangesAsync();
        return Ok(new { scheduledCount, finalDeadline = cursor });
    }

    public record AddExamQuestionDto(int TopicId, string QuestionType, string Prompt, string? CodeBlock, string? RawOptions, string CorrectAnswer, string? Explanation, string? SourceCitation);

    [HttpPost("exams/{examId:int}/questions")]
    public async Task<IActionResult> AddExamQuestion(int examId, [FromBody] AddExamQuestionDto dto)
    {
        string userEmail = _currentUser.Email;
        var exam = await _db.ChapterExams.FirstOrDefaultAsync(e =>
            e.Id == examId && (e.OwnerEmail == ChapterExam.TemplateOwner || e.OwnerEmail == userEmail));
        if (exam is null) return NotFound("Exam not found.");
        if (string.IsNullOrWhiteSpace(dto.Prompt) || string.IsNullOrWhiteSpace(dto.CorrectAnswer))
            return BadRequest("Prompt and CorrectAnswer are required.");

        int topicId = dto.TopicId;
        if (!await _db.Topics.AnyAsync(t => t.Id == topicId))
        {
            var fallbackTopic = exam.TrackModuleId.HasValue
                ? await _db.Topics.FirstOrDefaultAsync(t => t.TrackModuleId == exam.TrackModuleId.Value)
                : await _db.Topics.FirstOrDefaultAsync();
            if (fallbackTopic is null) return BadRequest("No valid Topic found.");
            topicId = fallbackTopic.Id;
        }

        var options = (dto.RawOptions ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim())
            .Where(o => o.Length > 0)
            .ToList();

        var q = new FlashcardQuestion
        {
            TopicId = topicId,
            ChapterExamId = exam.Id,
            QuestionType = string.IsNullOrWhiteSpace(dto.QuestionType) ? "MCQ" : dto.QuestionType.Trim(),
            Prompt = dto.Prompt.Trim(),
            CodeBlock = string.IsNullOrWhiteSpace(dto.CodeBlock) ? null : dto.CodeBlock,
            OptionsJson = options.Count > 0 ? JsonSerializer.Serialize(options) : null,
            CorrectAnswer = dto.CorrectAnswer.Trim(),
            Explanation = dto.Explanation?.Trim() ?? string.Empty,
            SourceCitation = string.IsNullOrWhiteSpace(dto.SourceCitation) ? $"Exam: {exam.Title}" : dto.SourceCitation.Trim()
        };
        _db.FlashcardQuestions.Add(q);
        await _db.SaveChangesAsync();
        return Ok(new { q.Id, q.ChapterExamId });
    }

    public record ExamSubmittedAnswerDto(int QuestionId, string? SelectedAnswer, bool? SelfMarkedCorrect);
    public record ExamSubmitRequestDto(int DurationSecondsUsed, List<ExamSubmittedAnswerDto> Answers);

    [HttpPost("exams/{examId:int}/submit")]
    public async Task<IActionResult> SubmitExam(int examId, [FromBody] ExamSubmitRequestDto dto)
    {
        string userEmail = _currentUser.Email;
        var exam = await _db.ChapterExams.FirstOrDefaultAsync(e =>
            e.Id == examId && (e.OwnerEmail == ChapterExam.TemplateOwner || e.OwnerEmail == userEmail));
        if (exam is null) return NotFound();

        var qIds = dto.Answers.Select(a => a.QuestionId).Distinct().ToList();
        var questions = await _db.FlashcardQuestions
            .AsNoTracking()
            .Include(q => q.Topic)
            .Where(q => qIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id);

        var breakdown = new List<ExamAnswerBreakdownItem>();
        int correctCount = 0;

        foreach (var ans in dto.Answers)
        {
            if (!questions.TryGetValue(ans.QuestionId, out var q)) continue;
            bool hasOptions = !string.IsNullOrWhiteSpace(q.OptionsJson);
            string chosen = (ans.SelectedAnswer ?? string.Empty).Trim();

            bool isCorrect = hasOptions
                ? string.Equals(chosen, q.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase)
                : (ans.SelfMarkedCorrect ?? false);

            if (isCorrect) correctCount++;

            _db.QuizAttempts.Add(new QuizAttempt
            {
                UserEmail = userEmail,
                FlashcardQuestionId = q.Id,
                TopicId = q.TopicId,
                SelectedAnswer = hasOptions ? chosen : (isCorrect ? "[Self-Verified Correct]" : "[Self-Marked Needs Review]"),
                IsCorrect = isCorrect,
                AttemptedAt = DateTime.UtcNow
            });

            breakdown.Add(new ExamAnswerBreakdownItem
            {
                QuestionId = q.Id,
                TopicId = q.TopicId,
                TopicSlug = q.Topic.Slug,
                TopicTitle = q.Topic.TitleEn,
                QuestionType = q.QuestionType,
                Prompt = q.Prompt,
                SelectedAnswer = hasOptions ? (string.IsNullOrEmpty(chosen) ? "(No Answer)" : chosen) : (isCorrect ? "Self-checked Correct" : "Needs Review"),
                CorrectAnswer = q.CorrectAnswer,
                IsCorrect = isCorrect,
                Explanation = q.Explanation,
                SourceCitation = q.SourceCitation
            });
        }

        int totalQ = breakdown.Count;
        int scorePct = totalQ == 0 ? 0 : (int)Math.Round(correctCount * 100.0 / totalQ);
        bool passed = totalQ > 0 && scorePct >= exam.PassingScorePercent;

        var submission = new ExamSubmission
        {
            UserEmail = userEmail,
            ChapterExamId = exam.Id,
            TotalQuestions = totalQ,
            CorrectAnswers = correctCount,
            ScorePercent = scorePct,
            Passed = passed,
            DurationSecondsUsed = Math.Max(0, dto.DurationSecondsUsed),
            SubmittedAt = DateTime.UtcNow,
            AnswersBreakdownJson = JsonSerializer.Serialize(breakdown)
        };
        _db.ExamSubmissions.Add(submission);

        var state = await _db.UserExamStates.FirstOrDefaultAsync(s => s.UserEmail == userEmail && s.ChapterExamId == exam.Id);
        if (state is null)
        {
            state = new UserExamState { UserEmail = userEmail, ChapterExamId = exam.Id };
            _db.UserExamStates.Add(state);
        }
        if (scorePct > state.BestScorePercent)
            state.BestScorePercent = scorePct;
        if (passed)
            state.IsCompleted = true;
        state.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            submissionId = submission.Id,
            scorePercent = scorePct,
            passed,
            correctAnswers = correctCount,
            totalQuestions = totalQ
        });
    }

    [HttpDelete("exams/{id:int}")]
    public async Task<IActionResult> DeleteExam(int id)
    {
        string userEmail = _currentUser.Email;
        var exam = await _db.ChapterExams.FirstOrDefaultAsync(e => e.Id == id);
        if (exam is null) return NotFound();
        if (exam.OwnerEmail != userEmail)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Default shared Chapter Exams cannot be deleted." });

        var customQs = await _db.FlashcardQuestions.Where(q => q.ChapterExamId == id).ToListAsync();
        foreach (var q in customQs) q.ChapterExamId = null;

        _db.ChapterExams.Remove(exam);
        await _db.SaveChangesAsync();
        return Ok(new { deleted = true });
    }

    private static string ResolveContentType(string ext) => ext switch
    {
        ".mp4" => "video/mp4",
        ".webm" => "video/webm",
        ".mkv" => "video/x-matroska",
        ".mov" => "video/quicktime",
        ".avi" => "video/x-msvideo",
        ".m4a" => "audio/mp4",
        ".mp3" => "audio/mpeg",
        ".wav" => "audio/wav",
        ".pdf" => "application/pdf",
        ".html" or ".htm" => "text/html; charset=utf-8",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".svg" => "image/svg+xml",
        ".txt" or ".md" or ".sql" or ".cs" or ".csproj" or ".sln" or ".slnx" or ".json" or ".xml" or ".yaml" or ".yml" or ".ps1" or ".bat" or ".css" or ".js"
            => "text/plain; charset=utf-8",
        _ => "application/octet-stream"
    };

    private static bool IsMediaExt(string ext) =>
        ext is ".mp4" or ".mkv" or ".webm" or ".mov" or ".avi" or ".m4a" or ".mp3" or ".wav";

    private static bool IsTextCodeExt(string ext) =>
        ext is ".cs" or ".sql" or ".txt" or ".md" or ".csproj" or ".sln" or ".slnx" or ".json" or ".xml" or ".yaml" or ".yml" or ".ps1" or ".bat" or ".css" or ".js";

    private static bool IsImageExt(string ext) =>
        ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".svg";

    private static bool CanRenderInlineInTab(string ext) =>
        ext is ".pdf" or ".html" or ".htm" or ".mp4" or ".webm" or ".m4a" or ".mp3" or ".wav"
        || IsImageExt(ext) || IsTextCodeExt(ext);

    private static DateTime? NormalizeUtcDate(DateTime? dt) =>
        dt.HasValue ? DateTime.SpecifyKind(dt.Value.Date, DateTimeKind.Utc) : null;
}
