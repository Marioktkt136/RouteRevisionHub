using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteRevisionHub.Data;
using RouteRevisionHub.Services;

namespace RouteRevisionHub.Controllers;

public class ArchitectureViewModel
{
    public List<MindMapModuleVm> Modules { get; set; } = new();
    public List<MindMapEdgeDto> CrossEdges { get; set; } = new();
    public List<ProcessFlowDto> ProcessFlows { get; set; } = new();
}

public class MindMapModuleVm
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "#38bdf8";
    public bool IsUpcoming { get; set; }
    public List<MindMapTopicVm> Topics { get; set; } = new();
}

public class MindMapTopicVm
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string SessionLabel { get; set; } = string.Empty;
    public string Priority { get; set; } = "Core";
    public string Status { get; set; } = "NotStarted";
    public List<string> TopKeys { get; set; } = new();
    public int VideoCount { get; set; }
    public int DocCount { get; set; }
}

[Authorize(Policy = "DriveGroupMember")]
public class ArchitectureController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public ArchitectureController(AppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(string? tab = "mindmap", string? flow = null)
    {
        string userEmail = _currentUser.Email;
        var rawModules = await _db.TrackModules
            .AsNoTracking()
            .Include(m => m.Topics).ThenInclude(t => t.Progresses.Where(p => p.UserEmail == userEmail))
            .Include(m => m.Topics).ThenInclude(t => t.ConventionKeys)
            .Include(m => m.Topics).ThenInclude(t => t.SourceRefs).ThenInclude(s => s.CourseResource)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();

        var modules = rawModules.Select(m => new MindMapModuleVm
        {
            Id = m.Id,
            Code = m.Code,
            TitleEn = m.TitleEn,
            TitleAr = m.TitleAr,
            BadgeColor = m.BadgeColor,
            IsUpcoming = m.IsUpcoming,
            Topics = m.Topics.OrderBy(t => t.DisplayOrder).Select(t =>
            {
                int vids = t.SourceRefs.Count(s => s.CourseResource.ResourceType is "Video" or "Audio");
                return new MindMapTopicVm
                {
                    Id = t.Id,
                    Slug = t.Slug,
                    TitleEn = t.TitleEn,
                    TitleAr = t.TitleAr,
                    SessionLabel = t.SessionLabel,
                    Priority = t.Priority,
                    Status = t.Progress?.Status ?? "NotStarted",
                    TopKeys = t.ConventionKeys.OrderByDescending(k => k.IsCritical).Take(5).Select(k => k.KeyName).ToList(),
                    VideoCount = vids,
                    DocCount = t.SourceRefs.Count - vids
                };
            }).ToList()
        }).ToList();

        ViewBag.ActiveTab = (tab ?? "mindmap").ToLowerInvariant();
        ViewBag.SelectedFlow = flow;

        return View(new ArchitectureViewModel
        {
            Modules = modules,
            CrossEdges = ArchitectureBlueprintData.GetTopicDependencies(),
            ProcessFlows = ArchitectureBlueprintData.GetProcessFlows()
        });
    }
}
