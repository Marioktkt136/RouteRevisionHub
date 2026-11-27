using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteRevisionHub.Data;
using RouteRevisionHub.Models;
using RouteRevisionHub.Services;
using RouteRevisionHub.ViewModels;

namespace RouteRevisionHub.Controllers;

[Authorize(Policy = "DriveGroupMember")]
public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly RevisionSearchService _search;
    private readonly ICurrentUserAccessor _currentUser;

    public HomeController(AppDbContext db, RevisionSearchService search, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _search = search;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index()
    {
        string userEmail = _currentUser.Email;

        var modules = await _db.TrackModules
            .AsNoTracking()
            .Include(m => m.Topics).ThenInclude(t => t.Progresses.Where(p => p.UserEmail == userEmail))
            .Include(m => m.Topics).ThenInclude(t => t.ConventionKeys)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();

        var allTopics = await _db.Topics
            .AsNoTracking()
            .Include(t => t.TrackModule)
            .Include(t => t.Progresses.Where(p => p.UserEmail == userEmail))
            .Include(t => t.ConventionKeys)
            .Include(t => t.SourceRefs).ThenInclude(s => s.CourseResource)
            .Include(t => t.Questions)
            .OrderBy(t => t.TrackModule.DisplayOrder)
            .ThenBy(t => t.DisplayOrder)
            .ToListAsync();

        var totalResources = await _db.CourseResources.CountAsync();
        var totalVideos = await _db.CourseResources.CountAsync(r => r.ResourceType == "Video" || r.ResourceType == "Audio");
        var totalVideoSeconds = await _db.CourseResources
            .Where(r => r.ResourceType == "Video" || r.ResourceType == "Audio")
            .SumAsync(r => (long)r.DurationSeconds);

        var quizTotal = await _db.QuizAttempts.CountAsync(a => a.UserEmail == userEmail);
        var quizCorrect = await _db.QuizAttempts.CountAsync(a => a.UserEmail == userEmail && a.IsCorrect);

        static TopicCardItem ToCard(Models.Entities.Topic t) => new()
        {
            Id = t.Id,
            Slug = t.Slug,
            TitleEn = t.TitleEn,
            TitleAr = t.TitleAr,
            ModuleTitleEn = t.TrackModule.TitleEn,
            ModuleCode = t.TrackModule.Code,
            BadgeColor = t.TrackModule.BadgeColor,
            SessionLabel = t.SessionLabel,
            Priority = t.Priority,
            Status = t.Progress?.Status ?? "NotStarted",
            ConfidenceLevel = t.Progress?.ConfidenceLevel ?? 1,
            KeyCount = t.ConventionKeys.Count,
            SourceCount = t.SourceRefs.Count,
            VideoCount = t.SourceRefs.Count(s => s.CourseResource.ResourceType is "Video" or "Audio"),
            QuestionCount = t.Questions.Count,
            TopKeys = t.ConventionKeys.OrderByDescending(k => k.IsCritical).Take(4).Select(k => k.KeyName).ToList()
        };

        var vm = new DashboardViewModel
        {
            TotalModules = modules.Count(m => !m.IsUpcoming),
            TotalTopics = allTopics.Count(t => !t.TrackModule.IsUpcoming),
            MasteredTopics = allTopics.Count(t => t.Progress?.Status == "Mastered"),
            InProgressTopics = allTopics.Count(t => t.Progress?.Status == "InProgress"),
            NeedsReviewTopics = allTopics.Count(t => t.Progress?.Status == "NeedsReview"),
            TotalConventionKeys = allTopics.Sum(t => t.ConventionKeys.Count),
            TotalQuestions = allTopics.Sum(t => t.Questions.Count),
            TotalVideos = totalVideos,
            TotalVideoHours = Math.Round(totalVideoSeconds / 3600m, 2),
            TotalResources = totalResources,
            QuizCorrectCount = quizCorrect,
            QuizTotalCount = quizTotal,
            Modules = modules.Select(m => new ModuleSummaryItem
            {
                Id = m.Id,
                Code = m.Code,
                TitleEn = m.TitleEn,
                TitleAr = m.TitleAr,
                Description = m.Description,
                BadgeColor = m.BadgeColor,
                TotalVideoCount = m.TotalVideoCount,
                TotalVideoHours = m.TotalVideoHours,
                TotalFileCount = m.TotalFileCount,
                IsUpcoming = m.IsUpcoming,
                TopicCount = m.Topics.Count,
                MasteredCount = m.Topics.Count(t => t.Progress?.Status == "Mastered"),
                ConventionKeyCount = m.Topics.Sum(t => t.ConventionKeys.Count)
            }).ToList(),
            PriorityTopics = allTopics
                .Where(t => t.Priority == "InterviewCritical" && !t.TrackModule.IsUpcoming)
                .Take(12)
                .Select(ToCard)
                .ToList(),
            WeakOrRecentTopics = allTopics
                .Where(t => t.Progress != null && (t.Progress.Status == "NeedsReview" || t.Progress.Status == "InProgress"))
                .OrderByDescending(t => t.Progress!.Status == "NeedsReview")
                .ThenByDescending(t => t.Progress!.UpdatedAt)
                .Take(8)
                .Select(ToCard)
                .ToList()
        };

        return View(vm);
    }

    public async Task<IActionResult> Roadmap(int? moduleId, string? status, string? priority, string? q)
    {
        string userEmail = _currentUser.Email;
        var modules = await _db.TrackModules
            .AsNoTracking()
            .Include(m => m.Topics).ThenInclude(t => t.Progresses.Where(p => p.UserEmail == userEmail))
            .Include(m => m.Topics).ThenInclude(t => t.ConventionKeys)
            .Include(m => m.Topics).ThenInclude(t => t.Questions)
            .Include(m => m.Topics).ThenInclude(t => t.SourceRefs).ThenInclude(s => s.CourseResource)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();

        if (moduleId.HasValue)
            modules = modules.Where(m => m.Id == moduleId.Value).ToList();

        string qTrim = (q ?? string.Empty).Trim();
        foreach (var mod in modules)
        {
            var filtered = mod.Topics.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(status))
                filtered = filtered.Where(t => string.Equals(t.Progress?.Status ?? "NotStarted", status, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(priority))
                filtered = filtered.Where(t => string.Equals(t.Priority, priority, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(qTrim))
                filtered = filtered.Where(t =>
                    t.TitleEn.Contains(qTrim, StringComparison.OrdinalIgnoreCase)
                    || t.TitleAr.Contains(qTrim, StringComparison.OrdinalIgnoreCase)
                    || t.SessionLabel.Contains(qTrim, StringComparison.OrdinalIgnoreCase)
                    || t.ConventionKeys.Any(k => k.KeyName.Contains(qTrim, StringComparison.OrdinalIgnoreCase)));

            mod.Topics = filtered.OrderBy(t => t.DisplayOrder).ToList();
        }

        if (!string.IsNullOrWhiteSpace(qTrim) || !string.IsNullOrWhiteSpace(status) || !string.IsNullOrWhiteSpace(priority))
            modules = modules.Where(m => m.Topics.Count > 0).ToList();

        ViewBag.AllModules = await _db.TrackModules.AsNoTracking().OrderBy(m => m.DisplayOrder).ToListAsync();

        return View(new RoadmapViewModel
        {
            SelectedModuleId = moduleId,
            SelectedStatus = status,
            SelectedPriority = priority,
            Query = q,
            Modules = modules
        });
    }

    public async Task<IActionResult> Search(string? q, int? moduleId, string? resourceType, string? instructor)
    {
        ViewBag.Modules = await _db.TrackModules.AsNoTracking().OrderBy(m => m.DisplayOrder).ToListAsync();
        ViewBag.Instructors = await _db.CourseResources.AsNoTracking().Select(r => r.Instructor).Distinct().OrderBy(x => x).ToListAsync();
        ViewBag.ResourceTypes = await _db.CourseResources.AsNoTracking().Select(r => r.ResourceType).Distinct().OrderBy(x => x).ToListAsync();
        ViewBag.SelectedModuleId = moduleId;
        ViewBag.SelectedType = resourceType;
        ViewBag.SelectedInstructor = instructor;

        var results = await _search.SearchAsync(q, moduleId, resourceType, instructor);
        return View(results);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
