using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentApp.Web.Models.Entities;
using StudentApp.Web.Models.ViewModels;
using StudentApp.Web.Services;

namespace StudentApp.Web.Controllers;

public class ActivitiesController : Controller
{
    private readonly IGroupService _groupService;
    private readonly IActivityService _activityService;
    private readonly IAssignmentService _assignmentService;

    public ActivitiesController(IGroupService groupService, IActivityService activityService, IAssignmentService assignmentService)
    {
        _groupService = groupService;
        _activityService = activityService;
        _assignmentService = assignmentService;
    }

    private async Task PopulateActiveGroupAsync()
    {
        var groups = await _groupService.GetNonArchivedGroupsAsync();
        ViewBag.AllGroups = groups.Select(g => new { g.Id, g.Name }).ToList();
        var activeId = HttpContext.Session.GetActiveGroup();
        if (activeId.HasValue)
        {
            var name = groups.FirstOrDefault(g => g.Id == activeId.Value)?.Name;
            ViewData["ActiveGroupName"] = name;
        }
    }

    public async Task<IActionResult> Index()
    {
        await PopulateActiveGroupAsync();
        var activeGroupId = HttpContext.Session.GetActiveGroup();

        if (!activeGroupId.HasValue)
        {
            ViewBag.NoGroupSelected = true;
            return View(new List<ActivitySummaryVm>());
        }

        var activities = await _activityService.GetActivitySummariesAsync(activeGroupId.Value);
        return View(activities);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateActiveGroupAsync();
        var activeGroupId = HttpContext.Session.GetActiveGroup();

        if (!activeGroupId.HasValue)
        {
            TempData["Error"] = "Najskôr vyberte skupinu.";
            return RedirectToAction("Index", "Groups");
        }

        var groupInfo = await _activityService.GetGroupInfoAsync(activeGroupId.Value);

        if (groupInfo == null)
        {
            TempData["Error"] = "Vybraná skupina sa nenašla.";
            return RedirectToAction("Index", "Groups");
        }

        var vm = new ActivityCreateVm
        {
            GroupId = groupInfo.Value.Id,
            GroupName = groupInfo.Value.Name
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ActivityCreateVm vm)
    {
        var activeGroupId = HttpContext.Session.GetActiveGroup() ?? (vm.GroupId > 0 ? vm.GroupId : (int?)null);
        if (!activeGroupId.HasValue)
        {
            TempData["Error"] = "Najskôr vyberte skupinu.";
            return RedirectToAction("Index", "Groups");
        }

        ModelState.Remove(nameof(ActivityCreateVm.GroupId));
        ModelState.Remove(nameof(ActivityCreateVm.GroupName));

        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            var groupName = await _groupService.GetGroupNameAsync(activeGroupId.Value);
            vm.GroupId = activeGroupId.Value;
            vm.GroupName = groupName ?? "Neznáma";
            return View(vm);
        }

        var activity = await _activityService.CreateActivityAsync(vm.Name, vm.Description, activeGroupId.Value);

        TempData["Success"] = $"Aktivita '{activity.Name}' bola úspešne vytvorená.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        await PopulateActiveGroupAsync();
        var activity = await _activityService.GetActivityByIdAsync(id);
        if (activity == null) return NotFound();

        return View(new ActivityEditVm
        {
            Id = activity.Id,
            Name = activity.Name,
            Description = activity.Description,
            GroupId = activity.GroupId
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ActivityEditVm vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        var activity = await _activityService.UpdateActivityAsync(id, vm.Name, vm.Description);
        if (activity == null) return NotFound();

        TempData["Success"] = $"Aktivita '{activity.Name}' bola aktualizovaná.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        await PopulateActiveGroupAsync();
        var result = await _activityService.GetActivityDetailsAsync(id);
        if (result == null) return NotFound();

        ViewBag.AllStudents = result.Value.AllStudents;
        return View(result.Value.Details);
    }

    [HttpPost]
    public async Task<IActionResult> Archive(int id)
    {
        var (success, message) = await _activityService.ArchiveActivityAsync(id);
        return Json(new { success, message });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var (exists, _) = await _activityService.DeleteActivityAsync(id);
        if (!exists)
            return Json(new { success = false, message = "Aktivita nebola nájdená." });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Assign(int id)
    {
        try
        {
            var result = await _assignmentService.AssignStudentsAsync(id);
            TempData["Success"] = $"Priradených {result.Assignments.Count} študentov k aktivite.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Priradenie zlyhalo: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> BulkAssign([FromBody] int[]? activityIds)
    {
        if (activityIds == null || activityIds.Length == 0)
            return BadRequest("Žiadne aktivity neboli vybrané.");
        try
        {
            await _assignmentService.BulkAssignAsync(activityIds);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> BulkDelete([FromBody] int[]? activityIds)
    {
        if (activityIds == null || activityIds.Length == 0)
            return Json(new { success = false, message = "Žiadne aktivity neboli vybrané." });

        await _activityService.BulkDeleteActivitiesAsync(activityIds.ToList());
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> GetEligibleStudents(int activityId, bool includeAlreadyAssigned = false)
    {
        var eligible = await _activityService.GetEligibleStudentsAsync(activityId, includeAlreadyAssigned);
        if (eligible == null) return NotFound();

        return Json(eligible);
    }

    [HttpPost]
    public async Task<IActionResult> DrawForActivity(int activityId, int count)
    {
        try
        {
            var drawn = await _assignmentService.DrawAddForActivityAsync(activityId, count);
            TempData["DrawnStudents"] = JsonSerializer.Serialize(
                drawn.Select(s => $"{s.FirstName} {s.LastName}").ToList());
            return RedirectToAction(nameof(DrawResult), new { activityId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Žrebovanie zlyhalo: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> DrawResult(int activityId)
    {
        var drawnJson = TempData["DrawnStudents"] as string;
        if (drawnJson == null)
            return RedirectToAction(nameof(Details), new { id = activityId });

        var drawnNames = JsonSerializer.Deserialize<List<string>>(drawnJson)!;

        var data = await _activityService.GetDrawResultDataAsync(activityId);
        if (data == null) return NotFound();

        var vm = new DrawResultVm
        {
            ActivityId = activityId,
            ActivityName = data.Value.ActivityName,
            SourceName = data.Value.ActivityName,
            DrawTypeName = "Aktivita",
            DrawnStudentNames = drawnNames,
            AllStudentNames = data.Value.AllStudentNames
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> DrawForPresentation(int taskId, int count)
    {
        try
        {
            var (drawn, _) = await _assignmentService.DrawAddForPresentationAsync(taskId, count, PresentationRole.Presentee);
            TempData["DrawnStudents"] = JsonSerializer.Serialize(
                drawn.Select(s => $"{s.FirstName} {s.LastName}").ToList());
            return RedirectToAction(nameof(DrawResultForPresentation), new { taskId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Žrebovanie zlyhalo: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> DrawResultForPresentation(int taskId)
    {
        var drawnJson = TempData["DrawnStudents"] as string;

        var data = await _activityService.GetPresentationDrawResultDataAsync(taskId);
        if (data == null) return NotFound();

        if (drawnJson == null)
            return RedirectToAction(nameof(Details), new { id = data.Value.ActivityId });

        var drawnNames = JsonSerializer.Deserialize<List<string>>(drawnJson)!;

        var vm = new DrawResultVm
        {
            ActivityId = data.Value.ActivityId,
            ActivityName = data.Value.ActivityName,
            SourceName = data.Value.TaskTitle,
            DrawTypeName = "Prezentácia",
            DrawnStudentNames = drawnNames,
            AllStudentNames = data.Value.AllStudentNames
        };

        return View("DrawResult", vm);
    }

    [HttpPost]
    public async Task<IActionResult> Duplicate(int id)
    {
        var (success, newId, message) = await _activityService.DuplicateActivityAsync(id);
        if (!success)
            return Json(new { success = false, message });

        return Json(new { success = true, newId });
    }

    [HttpPost]
    public async Task<IActionResult> SetActivityAssignments(int activityId, int[]? studentIds)
    {
        var found = await _activityService.SetActivityAssignmentsAsync(activityId, studentIds);
        if (!found) return NotFound();

        return Ok();
    }
}
