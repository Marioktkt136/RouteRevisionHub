using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteRevisionHub.Data;
using RouteRevisionHub.ViewModels;

namespace RouteRevisionHub.Controllers;

[Authorize(Policy = "DriveGroupMember")]
public class ResourcesController : Controller
{
    private readonly AppDbContext _db;

    public ResourcesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, string? moduleFolder, string? resourceType, string? instructor, int page = 1)
    {
        const int pageSize = 40;
        page = Math.Max(1, page);

        var allModules = await _db.CourseResources.AsNoTracking().Select(r => r.ModuleFolder).Distinct().OrderBy(x => x).ToListAsync();
        var allTypes = await _db.CourseResources.AsNoTracking().Select(r => r.ResourceType).Distinct().OrderBy(x => x).ToListAsync();
        var allInstructors = await _db.CourseResources.AsNoTracking().Select(r => r.Instructor).Distinct().OrderBy(x => x).ToListAsync();

        var query = _db.CourseResources
            .AsNoTracking()
            .Include(r => r.TopicRefs).ThenInclude(s => s.Topic)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            string like = $"%{q.Trim()}%";
            bool isPg = _db.Database.IsNpgsql();
            query = isPg
                ? query.Where(r => EF.Functions.ILike(r.FileName, like)
                                || EF.Functions.ILike(r.RelativePath, like)
                                || EF.Functions.ILike(r.SearchableText, like))
                : query.Where(r => EF.Functions.Like(r.FileName, like)
                                || EF.Functions.Like(r.RelativePath, like)
                                || EF.Functions.Like(r.SearchableText, like));
        }

        if (!string.IsNullOrWhiteSpace(moduleFolder))
            query = query.Where(r => r.ModuleFolder == moduleFolder);

        if (!string.IsNullOrWhiteSpace(resourceType))
            query = query.Where(r => r.ResourceType == resourceType);

        if (!string.IsNullOrWhiteSpace(instructor))
            query = query.Where(r => r.Instructor == instructor);

        int totalCount = await query.CountAsync();
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Min(page, totalPages);

        var items = await query
            .OrderBy(r => r.ModuleFolder)
            .ThenBy(r => r.ResourceType == "Video" ? 0 : 1)
            .ThenBy(r => r.RelativePath)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(new ResourcesLibraryViewModel
        {
            Query = q,
            ModuleFolder = moduleFolder,
            ResourceType = resourceType,
            Instructor = instructor,
            Page = page,
            TotalCount = totalCount,
            TotalPages = totalPages,
            AllModules = allModules,
            AllTypes = allTypes,
            AllInstructors = allInstructors,
            Items = items
        });
    }
}
