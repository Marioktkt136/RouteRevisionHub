using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteRevisionHub.Data;
using RouteRevisionHub.Models.Entities;
using RouteRevisionHub.Services;

namespace RouteRevisionHub.Controllers;

public class ChecklistViewModel
{
    public string? Filter { get; set; }
    public int? ModuleId { get; set; }
    public int TotalTopics { get; set; }
    public int MasteredTopics { get; set; }
    public int TotalSteps { get; set; }
    public int CompletedSteps { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverallPercent => (TotalSteps + TotalTasks) == 0 ? 0
        : (int)Math.Round((CompletedSteps + CompletedTasks) * 100.0 / (TotalSteps + TotalTasks));

    public List<TrackModule> AllModules { get; set; } = new();
    public List<TrackModule> VisibleModules { get; set; } = new();
    public List<ChecklistTask> Tasks { get; set; } = new();
}

[Authorize(Policy = "DriveGroupMember")]
public class ChecklistController : Controller
{
    private readonly AppDbContext _db;
    private readonly DatabaseSeeder _seeder;
    private readonly ICurrentUserAccessor _currentUser;

    public ChecklistController(AppDbContext db, DatabaseSeeder seeder, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _seeder = seeder;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(string? filter = "all", int? moduleId = null)
    {
        string userEmail = _currentUser.Email;
        await _seeder.EnsureUserSeededAsync(userEmail);

        var allModules = await _db.TrackModules
            .AsNoTracking()
            .Include(m => m.Topics).ThenInclude(t => t.Progresses.Where(p => p.UserEmail == userEmail))
            .Include(m => m.Topics).ThenInclude(t => t.ConventionKeys)
            .Include(m => m.Topics).ThenInclude(t => t.SourceRefs).ThenInclude(s => s.CourseResource)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();

        var allUserTasks = await _db.ChecklistTasks
            .AsNoTracking()
            .Include(t => t.TrackModule)
            .Where(t => t.UserEmail == userEmail)
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => t.DisplayOrder)
            .ThenBy(t => t.Id)
            .ToListAsync();

        var activeTopics = allModules.Where(m => !m.IsUpcoming).SelectMany(m => m.Topics).ToList();
        int totalTopics = activeTopics.Count;
        int masteredTopics = activeTopics.Count(t => t.Progress?.Status == "Mastered");
        int totalSteps = totalTopics * 3;
        int completedSteps = activeTopics.Sum(t =>
            (t.Progress?.VideoWatched == true ? 1 : 0) +
            (t.Progress?.SlidesReviewed == true ? 1 : 0) +
            (t.Progress?.PracticeDone == true ? 1 : 0));

        string normFilter = (filter ?? "all").ToLowerInvariant();
        var visible = new List<TrackModule>();
        foreach (var m in allModules)
        {
            if (moduleId.HasValue && m.Id != moduleId.Value) continue;
            var filteredTopics = m.Topics.OrderBy(t => t.DisplayOrder).Where(t =>
            {
                string st = t.Progress?.Status ?? "NotStarted";
                return normFilter switch
                {
                    "remaining" => st != "Mastered",
                    "inprogress" => st == "InProgress" || st == "NeedsReview",
                    "mastered" => st == "Mastered",
                    _ => true
                };
            }).ToList();

            if (filteredTopics.Count > 0 || normFilter == "all")
            {
                visible.Add(new TrackModule
                {
                    Id = m.Id,
                    Code = m.Code,
                    TitleEn = m.TitleEn,
                    TitleAr = m.TitleAr,
                    Description = m.Description,
                    DisplayOrder = m.DisplayOrder,
                    BadgeColor = m.BadgeColor,
                    TotalVideoCount = m.TotalVideoCount,
                    TotalVideoHours = m.TotalVideoHours,
                    TotalFileCount = m.TotalFileCount,
                    IsUpcoming = m.IsUpcoming,
                    Topics = filteredTopics
                });
            }
        }

        var filteredTasks = allUserTasks.AsEnumerable();
        if (moduleId.HasValue)
            filteredTasks = filteredTasks.Where(x => x.TrackModuleId == moduleId.Value || x.TrackModuleId == null);
        if (normFilter == "remaining")
            filteredTasks = filteredTasks.Where(x => !x.IsCompleted);
        else if (normFilter == "mastered")
            filteredTasks = filteredTasks.Where(x => x.IsCompleted);

        return View(new ChecklistViewModel
        {
            Filter = normFilter,
            ModuleId = moduleId,
            TotalTopics = totalTopics,
            MasteredTopics = masteredTopics,
            TotalSteps = totalSteps,
            CompletedSteps = completedSteps,
            TotalTasks = allUserTasks.Count,
            CompletedTasks = allUserTasks.Count(x => x.IsCompleted),
            AllModules = allModules.Select(m => new TrackModule { Id = m.Id, Code = m.Code, TitleEn = m.TitleEn }).ToList(),
            VisibleModules = visible,
            Tasks = filteredTasks.ToList()
        });
    }
}
