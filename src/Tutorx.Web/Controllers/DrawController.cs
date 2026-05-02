using Microsoft.AspNetCore.Mvc;
using Tutorx.Web.Models.Entities;
using Tutorx.Web.Models.ViewModels;
using Tutorx.Web.Services;

namespace Tutorx.Web.Controllers;

public class DrawController : Controller
{
    private readonly IGroupService _groupService;
    private readonly IActivityService _activityService;
    private readonly IDrawService _drawService;
    private readonly IAssignmentService _assignmentService;
    private readonly ILogger<DrawController> _logger;

    public DrawController(IGroupService groupService, IActivityService activityService, IDrawService drawService, IAssignmentService assignmentService, ILogger<DrawController> logger)
    {
        _groupService = groupService;
        _activityService = activityService;
        _drawService = drawService;
        _assignmentService = assignmentService;
        _logger = logger;
    }

    private async Task PopulateActiveGroupAsync()
    {
        var groups = await _groupService.GetNonArchivedGroupsAsync();
        ViewBag.AllGroups = groups.Select(g => new { g.Id, g.Name }).ToList();
        var activeId = HttpContext.Session.GetActiveGroup();
        if (activeId.HasValue)
            ViewData["ActiveGroupName"] = groups.FirstOrDefault(g => g.Id == activeId.Value)?.Name;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? groupId, string? activityIds = null, string? presentationIds = null, string? presRole = null)
    {
        var gid = groupId ?? HttpContext.Session.GetActiveGroup();
        if (!gid.HasValue)
        {
            await PopulateActiveGroupAsync();
            ViewBag.NoGroupSelected = true;
            return View((DrawIndexVm?)null);
        }

        var group = await _groupService.GetGroupByIdAsync(gid.Value);
        if (group == null) return NotFound();

        var allNames = await _activityService.GetActiveStudentNamesForGroupAsync(gid.Value);
        var history = await _drawService.GetHistoryAsync(gid.Value, 1);

        var allGroups = (await _groupService.GetNonArchivedGroupsAsync())
            .Select(g => new GroupSummaryVm { Id = g.Id, Name = g.Name })
            .ToList();

        var activities = await _activityService.GetActivitiesForGroupAsync(gid.Value);
        var presentations = await _activityService.GetPresentationsForGroupAsync(gid.Value);

        var vm = new DrawIndexVm
        {
            GroupId = gid.Value,
            GroupName = group.Name,
            AllActiveStudentNames = allNames,
            LastDraw = history.FirstOrDefault(),
            AllGroups = allGroups,
            Activities = activities,
            Presentations = presentations
        };

        var initialIds = string.IsNullOrEmpty(activityIds)
            ? new List<int>()
            : activityIds.Split(',')
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();
        ViewBag.InitialActivityIds = initialIds;

        var initialPresIds = string.IsNullOrEmpty(presentationIds)
            ? new List<int>()
            : presentationIds.Split(',')
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();
        ViewBag.InitialPresentationIds = initialPresIds;
        // presRole: "0" = Presentee, "1" = Substitution, "both" = both, null/other = default (Presentee)
        ViewBag.InitialPresRole = presRole ?? "0";

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> DrawForActivity(int activityId, int count, bool includeAlreadyAssigned = false, [FromForm] List<int>? allowedStudentIds = null)
    {
        _logger.LogInformation("DrawForActivity called: activityId={ActivityId}, count={Count}, includeAlreadyAssigned={Include}, allowedStudentIds=[{Ids}]",
            activityId, count, includeAlreadyAssigned, string.Join(",", allowedStudentIds ?? []));
        try
        {
            var drawn = await _assignmentService.DrawAddForActivityAsync(activityId, count, includeAlreadyAssigned, allowedStudentIds);
            var names = drawn.Select(s => $"{s.FirstName} {s.LastName}").ToList();
            return Json(new { success = true, drawnNames = names });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DrawForActivity failed: activityId={ActivityId}", activityId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DrawForPresentation(int taskId, int count, int role = 0, bool includeAlreadyAssigned = false, [FromForm] List<int>? allowedStudentIds = null, int? batchId = null)
    {
        _logger.LogInformation("DrawForPresentation called: taskId={TaskId}, count={Count}, role={Role}, includeAlreadyAssigned={Include}",
            taskId, count, role, includeAlreadyAssigned);
        try
        {
            var (drawn, usedBatchId) = await _assignmentService.DrawAddForPresentationAsync(taskId, count, (PresentationRole)role, includeAlreadyAssigned, allowedStudentIds, batchId);
            var names = drawn.Select(s => $"{s.FirstName} {s.LastName}").ToList();
            return Json(new { success = true, drawnNames = names, batchId = usedBatchId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DrawForPresentation failed: taskId={TaskId}", taskId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> History(int? groupId, int page = 1)
    {
        await PopulateActiveGroupAsync();

        var gid = groupId ?? HttpContext.Session.GetActiveGroup();
        if (!gid.HasValue)
        {
            TempData["Error"] = "Najskôr vyberte skupinu.";
            return RedirectToAction("Index", "Groups");
        }

        var group = await _groupService.GetGroupByIdAsync(gid.Value);
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
}
