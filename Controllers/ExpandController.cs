using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteRevisionHub.Data;
using RouteRevisionHub.Models.Entities;
using RouteRevisionHub.Services;

namespace RouteRevisionHub.Controllers;

public class ExpandViewModel
{
    public string CourseRootPath { get; set; } = string.Empty;
    public int ModuleCount { get; set; }
    public int TopicCount { get; set; }
    public int ResourceCount { get; set; }
    public int KeyCount { get; set; }
    public int UnlinkedCount { get; set; }
    public List<TrackModule> Modules { get; set; } = new();
    public List<Topic> AllTopics { get; set; } = new();
    public List<CourseResource> UnlinkedResources { get; set; } = new();
}

[Authorize(Policy = "DriveGroupMember")]
public class ExpandController : Controller
{
    private readonly AppDbContext _db;
    private readonly GoogleDriveService _drive;

    public ExpandController(AppDbContext db, GoogleDriveService drive)
    {
        _db = db;
        _drive = drive;
    }

    public async Task<IActionResult> Index()
    {
        var roots = _drive.GetDriveOptions().RootFolders;
        string rootSummary = string.Join(" · ", roots.Select(r => $"{r.Label} ({r.FolderId})"));
        var modules = await _db.TrackModules.AsNoTracking().OrderBy(m => m.DisplayOrder).ToListAsync();
        var topics = await _db.Topics.AsNoTracking().Include(t => t.TrackModule)
            .OrderBy(t => t.TrackModule.DisplayOrder).ThenBy(t => t.DisplayOrder).ToListAsync();

        var unlinkedQuery = _db.CourseResources.AsNoTracking().Where(r => !r.TopicRefs.Any());
        int unlinkedCount = await unlinkedQuery.CountAsync();
        var unlinkedSample = await unlinkedQuery
            .OrderByDescending(r => r.Id)
            .Take(60)
            .ToListAsync();

        return View(new ExpandViewModel
        {
            CourseRootPath = rootSummary,
            ModuleCount = modules.Count,
            TopicCount = topics.Count,
            ResourceCount = await _db.CourseResources.CountAsync(),
            KeyCount = await _db.ConventionKeys.CountAsync(),
            UnlinkedCount = unlinkedCount,
            Modules = modules,
            AllTopics = topics,
            UnlinkedResources = unlinkedSample
        });
    }
}
