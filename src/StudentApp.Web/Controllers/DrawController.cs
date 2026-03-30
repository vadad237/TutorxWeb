using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.ViewModels;
using StudentApp.Web.Services;

namespace StudentApp.Web.Controllers;

public class DrawController : Controller
{
    private readonly AppDbContext _db;
    private readonly IDrawService _drawService;
    private readonly IAssignmentService _assignmentService;

    public DrawController(AppDbContext db, IDrawService drawService, IAssignmentService assignmentService)
    {
        _db = db;
        _drawService = drawService;
        _assignmentService = assignmentService;
    }

    private async Task PopulateActiveGroupAsync()
    {
        var groups = await _db.Groups.Where(g => !g.IsArchived)
            .Select(g => new { g.Id, g.Name }).ToListAsync();
        ViewBag.AllGroups = groups;
        var activeId = HttpContext.Session.GetActiveGroup();
        if (activeId.HasValue)
            ViewData["ActiveGroupName"] = groups.FirstOrDefault(g => g.Id == activeId.Value)?.Name;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? groupId, string? activityIds = null)
    {
        var gid = groupId ?? HttpContext.Session.GetActiveGroup();
        if (!gid.HasValue)
        {
            await PopulateActiveGroupAsync();
            ViewBag.NoGroupSelected = true;
            return View((DrawIndexVm?)null);
        }

        var group = await _db.Groups.FindAsync(gid.Value);
        if (group == null) return NotFound();

        var allNames = await _db.Students
            .Where(s => s.GroupId == gid.Value && s.IsActive)
            .Select(s => s.FirstName + " " + s.LastName)
            .ToListAsync();

        var bagStatus = await _drawService.GetBagStatusAsync(gid.Value);
        var history = await _drawService.GetHistoryAsync(gid.Value, 1);

        var allGroups = await _db.Groups.Where(g => !g.IsArchived)
            .Select(g => new GroupSummaryVm { Id = g.Id, Name = g.Name })
            .ToListAsync();

        var activities = await _db.Activities
            .Where(a => a.GroupId == gid.Value && !a.IsArchived)
            .OrderBy(a => a.Name)
            .Select(a => new DrawActivityVm
            {
                Id = a.Id,
                Name = a.Name
            })
            .ToListAsync();

        var vm = new DrawIndexVm
        {
            GroupId = gid.Value,
            GroupName = group.Name,
            AllActiveStudentNames = allNames,
            BagStatus = bagStatus,
            LastDraw = history.FirstOrDefault(),
            AllGroups = allGroups,
            Activities = activities
        };

        // Parse and validate initial activity IDs from query string
        var initialIds = string.IsNullOrEmpty(activityIds)
            ? new List<int>()
            : activityIds.Split(',')
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();
        ViewBag.InitialActivityIds = initialIds;

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> DrawForActivity(int activityId, int count, bool includeAlreadyAssigned = false)
    {
        try
        {
            var drawn = await _assignmentService.DrawAddForActivityAsync(activityId, count, includeAlreadyAssigned);
            var names = drawn.Select(s => $"{s.FirstName} {s.LastName}").ToList();
            return Json(new { success = true, drawnNames = names });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Next(int groupId)
    {
        try
        {
            var result = await _drawService.DrawNextAsync(groupId);
            return Json(new { success = true, studentId = result.StudentId, fullName = result.FullName, cycleNumber = result.CycleNumber, remainingInBag = result.RemainingInBag });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> History(int? groupId, int page = 1)
    {
        var groups = await _db.Groups.Where(g => !g.IsArchived)
            .Select(g => new { g.Id, g.Name }).ToListAsync();
        ViewBag.AllGroups = groups;
        var activeId = HttpContext.Session.GetActiveGroup();
        if (activeId.HasValue)
            ViewData["ActiveGroupName"] = groups.FirstOrDefault(g => g.Id == activeId.Value)?.Name;

        var gid = groupId ?? HttpContext.Session.GetActiveGroup();
        if (!gid.HasValue)
        {
            TempData["Error"] = "Najskôr vyberte skupinu.";
            return RedirectToAction("Index", "Groups");
        }

        var group = await _db.Groups.FindAsync(gid.Value);
        if (group == null) return NotFound();

        const int pageSize = 25;
        var allBatches = await _drawService.GetBatchHistoryAsync(gid.Value);
        var totalPages = (int)Math.Ceiling(allBatches.Count / (double)pageSize);
        var pagedBatches = allBatches.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var vm = new DrawHistoryVm
        {
            GroupId = gid.Value,
            GroupName = group.Name,
            Batches = pagedBatches,
            CurrentPage = page,
            TotalPages = Math.Max(totalPages, 1)
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Reset(int groupId)
    {
        try
        {
            await _drawService.ResetBagAsync(groupId);
            return Json(new { success = true, message = "Bag reset successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> BagStatus(int groupId)
    {
        var status = await _drawService.GetBagStatusAsync(groupId);
        return Json(new { remaining = status.Remaining, total = status.Total, currentCycle = status.CurrentCycle });
    }
}
