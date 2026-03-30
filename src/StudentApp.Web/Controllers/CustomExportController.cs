using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.ViewModels;
using StudentApp.Web.Services;

namespace StudentApp.Web.Controllers;

public class CustomExportController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICustomExportService _exportService;

    public CustomExportController(AppDbContext db, ICustomExportService exportService)
    {
        _db = db;
        _exportService = exportService;
    }

    private async Task PopulateActiveGroupAsync()
    {
        var groups = await _db.Groups.Where(g => !g.IsArchived)
            .Select(g => new { g.Id, g.Name }).ToListAsync();
        ViewBag.AllGroups = groups;
        var activeId = HttpContext.Session.GetActiveGroup();
        if (activeId.HasValue)
        {
            var name = groups.FirstOrDefault(g => g.Id == activeId.Value)?.Name;
            ViewData["ActiveGroupName"] = name;
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await PopulateActiveGroupAsync();

        var activeGroupId = HttpContext.Session.GetActiveGroup();
        if (!activeGroupId.HasValue)
        {
            ViewBag.NoGroupSelected = true;
            return View(new CustomExportIndexVm());
        }

        var group = await _db.Groups.FindAsync(activeGroupId.Value);
        if (group == null)
        {
            ViewBag.NoGroupSelected = true;
            return View(new CustomExportIndexVm());
        }

        var vm = new CustomExportIndexVm
        {
            GroupId   = group.Id,
            GroupName = group.Name,
            Request   = new CustomExportRequestVm { GroupId = group.Id }
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(CustomExportRequestVm request)
    {
        var activeGroupId = HttpContext.Session.GetActiveGroup();
        if (!activeGroupId.HasValue)
            return RedirectToAction(nameof(Index));

        request.GroupId = activeGroupId.Value;

        if (!request.IncludeStudents && !request.IncludeAttendance &&
            !request.IncludeActivities && !request.IncludeTasks && !request.IncludePresentations)
        {
            TempData["Error"] = "Vyberte aspoň jednu sekciu na export.";
            return RedirectToAction(nameof(Index));
        }

        var groupName = (await _db.Groups
            .Where(g => g.Id == activeGroupId.Value)
            .Select(g => g.Name)
            .FirstOrDefaultAsync()) ?? "export";

        var safeName = string.Concat(groupName.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        var timestamp = DateTime.Now.ToString("yyyyMMdd");

        var data = await _exportService.GenerateAsync(request);

        if (request.Format == "csv")
            return File(data, "text/csv", $"{safeName}_{timestamp}.csv");

        return File(
            data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{safeName}_{timestamp}.xlsx");
    }
}
