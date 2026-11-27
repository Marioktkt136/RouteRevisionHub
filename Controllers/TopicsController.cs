using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteRevisionHub.Data;
using RouteRevisionHub.Services;
using RouteRevisionHub.ViewModels;

namespace RouteRevisionHub.Controllers;

[Authorize(Policy = "DriveGroupMember")]
public class TopicsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public TopicsController(AppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return RedirectToAction("Roadmap", "Home");
        string userEmail = _currentUser.Email;

        var topic = await _db.Topics
            .AsNoTracking()
            .Include(t => t.TrackModule)
            .Include(t => t.Progresses.Where(p => p.UserEmail == userEmail))
            .Include(t => t.ConventionKeys.OrderByDescending(k => k.IsCritical).ThenBy(k => k.Id))
            .Include(t => t.Questions)
            .Include(t => t.SourceRefs.OrderByDescending(s => s.RelevanceScore))
                .ThenInclude(s => s.CourseResource)
            .FirstOrDefaultAsync(t => t.Slug == id);

        if (topic is null) return NotFound();

        var orderedSlugs = await _db.Topics
            .AsNoTracking()
            .Include(t => t.TrackModule)
            .OrderBy(t => t.TrackModule.DisplayOrder)
            .ThenBy(t => t.DisplayOrder)
            .Select(t => new { t.Id, t.Slug, t.TitleEn })
            .ToListAsync();

        int idx = orderedSlugs.FindIndex(x => x.Id == topic.Id);
        Models.Entities.Topic? prev = idx > 0
            ? new Models.Entities.Topic { Slug = orderedSlugs[idx - 1].Slug, TitleEn = orderedSlugs[idx - 1].TitleEn }
            : null;
        Models.Entities.Topic? next = idx >= 0 && idx + 1 < orderedSlugs.Count
            ? new Models.Entities.Topic { Slug = orderedSlugs[idx + 1].Slug, TitleEn = orderedSlugs[idx + 1].TitleEn }
            : null;

        var videos = topic.SourceRefs
            .Where(s => s.CourseResource.ResourceType is "Video" or "Audio")
            .OrderByDescending(s => s.RelevanceScore)
            .ToList();

        var docs = topic.SourceRefs
            .Where(s => s.CourseResource.ResourceType is "Presentation" or "PDF" or "Guide" or "HTML" or "Text")
            .OrderByDescending(s => s.RelevanceScore)
            .ToList();

        var codes = topic.SourceRefs
            .Where(s => s.CourseResource.ResourceType is "SQL" or "Code" or "Archive")
            .OrderByDescending(s => s.RelevanceScore)
            .ToList();

        var flashcards = topic.Questions.Select(q => new FlashcardViewItem
        {
            Id = q.Id,
            QuestionType = q.QuestionType,
            Prompt = q.Prompt,
            CodeBlock = q.CodeBlock,
            Options = string.IsNullOrWhiteSpace(q.OptionsJson)
                ? new()
                : (JsonSerializer.Deserialize<List<string>>(q.OptionsJson) ?? new()),
            CorrectAnswer = q.CorrectAnswer,
            Explanation = q.Explanation,
            SourceCitation = q.SourceCitation
        }).ToList();

        return View(new TopicDetailViewModel
        {
            Topic = topic,
            PreviousTopic = prev,
            NextTopic = next,
            VideoRefs = videos,
            DocAndSlideRefs = docs,
            CodeAndSqlRefs = codes,
            Flashcards = flashcards
        });
    }

    public async Task<IActionResult> Conventions(string? q, string? category, bool criticalOnly = false)
    {
        var categories = await _db.ConventionKeys
            .AsNoTracking()
            .Select(k => k.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var query = _db.ConventionKeys
            .AsNoTracking()
            .Include(k => k.Topic).ThenInclude(t => t.TrackModule)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            string like = $"%{q.Trim()}%";
            bool isPg = _db.Database.IsNpgsql();
            query = isPg
                ? query.Where(k => EF.Functions.ILike(k.KeyName, like)
                                || EF.Functions.ILike(k.ShortRule, like)
                                || EF.Functions.ILike(k.Topic.TitleEn, like))
                : query.Where(k => EF.Functions.Like(k.KeyName, like)
                                || EF.Functions.Like(k.ShortRule, like)
                                || EF.Functions.Like(k.Topic.TitleEn, like));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(k => k.Category == category);

        if (criticalOnly)
            query = query.Where(k => k.IsCritical);

        var keys = await query
            .OrderBy(k => k.Topic.TrackModule.DisplayOrder)
            .ThenBy(k => k.Topic.DisplayOrder)
            .ThenByDescending(k => k.IsCritical)
            .ThenBy(k => k.KeyName)
            .ToListAsync();

        return View(new ConventionGlossaryViewModel
        {
            Query = q,
            Category = category,
            CriticalOnly = criticalOnly,
            Categories = categories,
            Keys = keys
        });
    }

    public async Task<IActionResult> Quiz(int? moduleId, string? type)
    {
        var modules = await _db.TrackModules.AsNoTracking().OrderBy(m => m.DisplayOrder).ToListAsync();

        var qQuery = _db.FlashcardQuestions
            .AsNoTracking()
            .Include(q => q.Topic).ThenInclude(t => t.TrackModule)
            .AsQueryable();

        if (moduleId.HasValue)
            qQuery = qQuery.Where(q => q.Topic.TrackModuleId == moduleId.Value);

        if (!string.IsNullOrWhiteSpace(type))
            qQuery = qQuery.Where(q => q.QuestionType == type);

        var rawQuestions = await qQuery
            .OrderBy(q => q.Topic.TrackModule.DisplayOrder)
            .ThenBy(q => q.Topic.DisplayOrder)
            .ThenBy(q => q.Id)
            .ToListAsync();

        var rows = rawQuestions.Select(q => new QuizQuestionRow
        {
            Id = q.Id,
            TopicId = q.TopicId,
            TopicSlug = q.Topic.Slug,
            TopicTitle = q.Topic.TitleEn,
            ModuleCode = q.Topic.TrackModule.Code,
            QuestionType = q.QuestionType,
            Prompt = q.Prompt,
            CodeBlock = q.CodeBlock,
            Options = string.IsNullOrWhiteSpace(q.OptionsJson)
                ? new()
                : (JsonSerializer.Deserialize<List<string>>(q.OptionsJson) ?? new()),
            CorrectAnswer = q.CorrectAnswer,
            Explanation = q.Explanation,
            SourceCitation = q.SourceCitation
        }).ToList();

        return View(new QuizArenaViewModel
        {
            ModuleId = moduleId,
            QuestionType = type,
            Modules = modules,
            Questions = rows
        });
    }
}
