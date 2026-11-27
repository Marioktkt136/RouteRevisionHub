using Microsoft.EntityFrameworkCore;
using RouteRevisionHub.Data;

namespace RouteRevisionHub.Services;

public record SearchResponseDto(
    string Query,
    List<TopicHitDto> Topics,
    List<ConventionKeyHitDto> ConventionKeys,
    List<ResourceHitDto> Videos,
    List<ResourceHitDto> Files);

public record TopicHitDto(
    int Id,
    string Slug,
    string TitleEn,
    string TitleAr,
    string ModuleCode,
    string ModuleTitleEn,
    string BadgeColor,
    string SessionLabel,
    string Priority,
    string Status);

public record ConventionKeyHitDto(
    int Id,
    string KeyName,
    string Category,
    string ShortRule,
    bool IsCritical,
    string TopicSlug,
    string TopicTitleEn,
    string ModuleCode);

public record ResourceHitDto(
    int Id,
    string FileName,
    string RelativePath,
    string ResourceType,
    string Instructor,
    string ModuleFolder,
    string DurationFormatted,
    int? MatchedTimestampSeconds,
    string MatchedTimestampLabel,
    string Excerpt,
    List<LinkedTopicBadgeDto> LinkedTopics);

public record LinkedTopicBadgeDto(string Slug, string TitleEn);

/// <summary>
/// Provides fast MS SQL Server search across topics, convention keys, video OCR frames, and indexed course files.
/// </summary>
public class RevisionSearchService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public RevisionSearchService(AppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SearchResponseDto> SearchAsync(string? rawQuery, int? moduleId = null, string? resourceType = null, string? instructor = null)
    {
        string q = (rawQuery ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(q))
        {
            return new SearchResponseDto(q, new(), new(), new(), new());
        }

        string userEmail = _currentUser.Email;
        string like = $"%{q}%";
        bool isPg = _db.Database.IsNpgsql();

        // 1. Topics
        var topicQuery = _db.Topics
            .AsNoTracking()
            .Include(t => t.TrackModule)
            .Include(t => t.Progresses.Where(p => p.UserEmail == userEmail))
            .AsQueryable();

        topicQuery = isPg
            ? topicQuery.Where(t => EF.Functions.ILike(t.TitleEn, like)
                                 || EF.Functions.ILike(t.TitleAr, like)
                                 || EF.Functions.ILike(t.SummaryAr, like)
                                 || EF.Functions.ILike(t.SummaryEn, like)
                                 || EF.Functions.ILike(t.SessionLabel, like)
                                 || (t.CodeSnippet != null && EF.Functions.ILike(t.CodeSnippet, like))
                                 || t.ConventionKeys.Any(k => EF.Functions.ILike(k.KeyName, like)))
            : topicQuery.Where(t => EF.Functions.Like(t.TitleEn, like)
                                 || EF.Functions.Like(t.TitleAr, like)
                                 || EF.Functions.Like(t.SummaryAr, like)
                                 || EF.Functions.Like(t.SummaryEn, like)
                                 || EF.Functions.Like(t.SessionLabel, like)
                                 || (t.CodeSnippet != null && EF.Functions.Like(t.CodeSnippet, like))
                                 || t.ConventionKeys.Any(k => EF.Functions.Like(k.KeyName, like)));

        if (moduleId.HasValue)
            topicQuery = topicQuery.Where(t => t.TrackModuleId == moduleId.Value);

        var topics = await topicQuery
            .OrderBy(t => t.TrackModule.DisplayOrder)
            .ThenBy(t => t.DisplayOrder)
            .Take(20)
            .Select(t => new TopicHitDto(
                t.Id,
                t.Slug,
                t.TitleEn,
                t.TitleAr,
                t.TrackModule.Code,
                t.TrackModule.TitleEn,
                t.TrackModule.BadgeColor,
                t.SessionLabel,
                t.Priority,
                t.Progresses.Where(p => p.UserEmail == userEmail).Select(p => p.Status).FirstOrDefault() ?? "NotStarted"))
            .ToListAsync();

        // 2. Convention Keys
        var keysQuery = _db.ConventionKeys
            .AsNoTracking()
            .Include(k => k.Topic).ThenInclude(t => t.TrackModule)
            .AsQueryable();

        keysQuery = isPg
            ? keysQuery.Where(k => EF.Functions.ILike(k.KeyName, like)
                                || EF.Functions.ILike(k.ShortRule, like)
                                || (k.ExampleSnippet != null && EF.Functions.ILike(k.ExampleSnippet, like)))
            : keysQuery.Where(k => EF.Functions.Like(k.KeyName, like)
                                || EF.Functions.Like(k.ShortRule, like)
                                || (k.ExampleSnippet != null && EF.Functions.Like(k.ExampleSnippet, like)));

        if (moduleId.HasValue)
            keysQuery = keysQuery.Where(k => k.Topic.TrackModuleId == moduleId.Value);

        var keys = await (isPg
                ? keysQuery.OrderByDescending(k => EF.Functions.ILike(k.KeyName, like))
                : keysQuery.OrderByDescending(k => EF.Functions.Like(k.KeyName, like)))
            .ThenByDescending(k => k.IsCritical)
            .Take(25)
            .Select(k => new ConventionKeyHitDto(
                k.Id,
                k.KeyName,
                k.Category,
                k.ShortRule,
                k.IsCritical,
                k.Topic.Slug,
                k.Topic.TitleEn,
                k.Topic.TrackModule.Code))
            .ToListAsync();

        // 3. Resources (Videos + Docs/SQL/Code)
        var resQuery = _db.CourseResources
            .AsNoTracking()
            .Include(r => r.TopicRefs).ThenInclude(sr => sr.Topic)
            .AsQueryable();

        resQuery = isPg
            ? resQuery.Where(r => EF.Functions.ILike(r.FileName, like)
                               || EF.Functions.ILike(r.RelativePath, like)
                               || EF.Functions.ILike(r.SearchableText, like))
            : resQuery.Where(r => EF.Functions.Like(r.FileName, like)
                               || EF.Functions.Like(r.RelativePath, like)
                               || EF.Functions.Like(r.SearchableText, like));

        if (!string.IsNullOrWhiteSpace(resourceType))
            resQuery = resQuery.Where(r => r.ResourceType == resourceType);
        if (!string.IsNullOrWhiteSpace(instructor))
            resQuery = resQuery.Where(r => r.Instructor == instructor);

        var matchedResources = await (isPg
                ? resQuery.OrderByDescending(r => EF.Functions.ILike(r.FileName, like))
                : resQuery.OrderByDescending(r => EF.Functions.Like(r.FileName, like)))
            .ThenBy(r => r.RelativePath)
            .Take(50)
            .ToListAsync();

        var videoHits = new List<ResourceHitDto>();
        var fileHits = new List<ResourceHitDto>();

        foreach (var r in matchedResources)
        {
            var (ts, tsLabel, excerpt) = ExtractMatchSnippet(r.SearchableText, q);
            var badges = r.TopicRefs
                .OrderByDescending(x => x.RelevanceScore)
                .Take(3)
                .Select(x => new LinkedTopicBadgeDto(x.Topic.Slug, x.Topic.TitleEn))
                .ToList();

            var dto = new ResourceHitDto(
                r.Id,
                r.FileName,
                r.RelativePath,
                r.ResourceType,
                r.Instructor,
                r.ModuleFolder,
                r.DurationFormatted,
                ts,
                tsLabel,
                excerpt,
                badges);

            if (r.ResourceType is "Video" or "Audio")
                videoHits.Add(dto);
            else
                fileHits.Add(dto);
        }

        return new SearchResponseDto(
            q,
            topics,
            keys,
            videoHits.Take(25).ToList(),
            fileHits.Take(25).ToList());
    }

    /// <summary>
    /// Finds an excerpt around the matched query and extracts an embedded video timestamp marker like [04:15|t=255] if present.
    /// </summary>
    private static (int? TsSeconds, string TsLabel, string Excerpt) ExtractMatchSnippet(string text, string query)
    {
        if (string.IsNullOrEmpty(text))
            return (null, string.Empty, string.Empty);

        int idx = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            string head = text.Length > 180 ? text[..180] + "..." : text;
            return (null, string.Empty, head);
        }

        int start = Math.Max(0, idx - 80);
        int len = Math.Min(text.Length - start, 240);
        string excerpt = (start > 0 ? "..." : "") + text.Substring(start, len).Replace('\r', ' ').Replace('\n', ' ') + (start + len < text.Length ? "..." : "");

        // Look backwards up to 1200 chars for timestamp marker [@125s] or [mm:ss|t=125]
        int lookbackStart = Math.Max(0, idx - 1200);
        string sliceBefore = text.Substring(lookbackStart, idx - lookbackStart);
        int markerPos = sliceBefore.LastIndexOf("[@", StringComparison.Ordinal);
        if (markerPos >= 0)
        {
            int endBracket = sliceBefore.IndexOf(']', markerPos);
            if (endBracket > markerPos)
            {
                string marker = sliceBefore.Substring(markerPos + 2, endBracket - markerPos - 2);
                // Format expected: mm:ss|t=123 or 123s
                var parts = marker.Split('|');
                if (parts.Length == 2 && parts[1].StartsWith("t=") && int.TryParse(parts[1][2..], out int sec))
                {
                    return (sec, parts[0], excerpt);
                }
            }
        }

        return (null, string.Empty, excerpt);
    }
}
