using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;
using StudentApp.Web.Models.ViewModels;
using StudentApp.Web.Services;

namespace StudentApp.Web.Controllers;

public class ActivitiesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAssignmentService _assignmentService;

    public ActivitiesController(AppDbContext db, IAssignmentService assignmentService)
    {
        _db = db;
        _assignmentService = assignmentService;
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

    public async Task<IActionResult> Index()
    {
        await PopulateActiveGroupAsync();
        var activeGroupId = HttpContext.Session.GetActiveGroup();

        if (!activeGroupId.HasValue)
        {
            ViewBag.NoGroupSelected = true;
            return View(new List<ActivitySummaryVm>());
        }

        var rawActivities = await _db.Activities
            .Where(a => !a.IsArchived && a.GroupId == activeGroupId.Value)
            .Include(a => a.Group)
            .Include(a => a.Tasks)
            .Include(a => a.Assignments).ThenInclude(asgn => asgn.Student)
            .OrderBy(a => a.Name)
            .ToListAsync();

        var activities = rawActivities.Select(a => new ActivitySummaryVm
        {
            Id = a.Id,
            Name = a.Name,
            GroupName = a.Group.Name,
            GroupId = a.GroupId,
            TaskCount = a.Tasks.Count,
            AssignedCount = a.Assignments.Count,
            IsArchived = a.IsArchived,
            AssignedStudents = a.Assignments
                .Select(asgn => asgn.Student.FirstName + " " + asgn.Student.LastName)
                .Distinct()
                .OrderBy(name => name)
                .ToList()
        }).ToList();

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

        var group = await _db.Groups.Where(g => g.Id == activeGroupId.Value && !g.IsArchived)
            .Select(g => new { g.Id, g.Name }).FirstOrDefaultAsync();

        if (group == null)
        {
            TempData["Error"] = "Vybraná skupina sa nenašla.";
            return RedirectToAction("Index", "Groups");
        }

        var vm = new ActivityCreateVm
        {
            GroupId = group.Id,
            GroupName = group.Name
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityCreateVm vm)
    {
        // Resolve group: prefer session, fall back to posted GroupId
        var activeGroupId = HttpContext.Session.GetActiveGroup() ?? (vm.GroupId > 0 ? vm.GroupId : (int?)null);
        if (!activeGroupId.HasValue)
        {
            TempData["Error"] = "Najskôr vyberte skupinu.";
            return RedirectToAction("Index", "Groups");
        }

        // Clear server-controlled fields from ModelState
        ModelState.Remove(nameof(ActivityCreateVm.GroupId));
        ModelState.Remove(nameof(ActivityCreateVm.GroupName));

        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            var group = await _db.Groups.Where(g => g.Id == activeGroupId.Value)
                .Select(g => g.Name).FirstOrDefaultAsync();
            vm.GroupId = activeGroupId.Value;
            vm.GroupName = group ?? "Unknown";
            return View(vm);
        }

        var activity = new Activity
        {
            Name = vm.Name.Trim(),
            Description = vm.Description?.Trim(),
            GroupId = activeGroupId.Value
        };
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Aktivita '{activity.Name}' bola úspešne vytvorená.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        await PopulateActiveGroupAsync();
        var activity = await _db.Activities.FindAsync(id);
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ActivityEditVm vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        var activity = await _db.Activities.FindAsync(id);
        if (activity == null) return NotFound();

        activity.Name = vm.Name.Trim();
        activity.Description = vm.Description?.Trim();
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Aktivita '{activity.Name}' bola aktualizovaná.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        await PopulateActiveGroupAsync();
        var activity = await _db.Activities
            .Include(a => a.Group).ThenInclude(g => g.Students)
            .Include(a => a.Tasks).ThenInclude(t => t.PresentationStudents).ThenInclude(ps => ps.Student)
            .Include(a => a.Assignments).ThenInclude(a => a.Student)
            .Include(a => a.OtherAttributes).ThenInclude(attr => attr.Options)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (activity == null) return NotFound();

        var activeStudentCount = activity.Group.Students.Count(s => s.IsActive);
        var assignedStudentCount = activity.Assignments.Select(a => a.StudentId).Distinct().Count();

        var vm = new ActivityDetailsVm
        {
            Id = activity.Id,
            Name = activity.Name,
            Description = activity.Description,
            GroupId = activity.GroupId,
            GroupName = activity.Group.Name,
            IsArchived = activity.IsArchived,
            Tasks = activity.Tasks
                .Where(t => !t.IsPresentation)
                .OrderBy(t => t.Title)
                .Select(t => new SimpleTaskVm(t.Id, t.Title))
                .ToList(),
            Presentations = activity.Tasks
                .Where(t => t.IsPresentation)
                .OrderBy(t => t.Title)
                .Select(t => new TaskWithAssignmentVm(
                    t.Id,
                    t.Title,
                    t.PresentationDate,
                    t.PresentationStudents
                        .Select(ps => new PresentationStudentVm(ps.StudentId, ps.Student.FirstName + " " + ps.Student.LastName))
                        .OrderBy(ps => ps.FullName)
                        .ToList()
                )).ToList(),
            AssignedStudents = activity.Assignments.Select(a => new AssignedStudentVm(
                a.Id,
                a.StudentId,
                a.Student.FirstName + " " + a.Student.LastName
            )).OrderBy(s => s.FullName).ToList(),
            UnassignedStudentCount = activeStudentCount - assignedStudentCount,
            UnassignedTaskCount = 0,
            OtherAttributes = activity.OtherAttributes
                .OrderBy(a => a.Id)
                .Select(a => new OtherAttributeVm(
                    a.Id,
                    a.Name,
                    a.Options.OrderBy(o => o.Id)
                        .Select(o => new AttributeOptionVm(o.Id, o.Name, o.Color))
                        .ToList()
                )).ToList()
        };

        // Load student attribute values for this activity's attributes
        var attrIds = vm.OtherAttributes.Select(a => a.AttributeId).ToList();
        var studentIds = vm.AssignedStudents.Select(s => s.StudentId).ToList();
        if (attrIds.Any() && studentIds.Any())
        {
            var values = await _db.StudentAttributeValues
                .Include(v => v.Option)
                .Where(v => attrIds.Contains(v.ActivityAttributeId) && studentIds.Contains(v.StudentId))
                .ToListAsync();
            vm.AttributeValues = values.Select(v => new StudentAttributeValueVm(
                v.StudentId, v.ActivityAttributeId, v.OptionId, v.Option?.Name, v.Option?.Color
            )).ToList();
        }

        ViewBag.AllStudents = activity.Group.Students
            .Where(s => s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new StudentSummaryVm { Id = s.Id, FullName = s.FirstName + " " + s.LastName })
            .ToList();

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Archive(int id)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity == null)
            return Json(new { success = false, message = "Activity not found." });

        activity.IsArchived = true;
        await _db.SaveChangesAsync();
        return Json(new { success = true, message = $"Activity '{activity.Name}' archived." });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var exists = await _db.Activities.AnyAsync(a => a.Id == id);
        if (!exists)
            return Json(new { success = false, message = "Activity not found." });

        var taskIds = await _db.TaskItems.Where(t => t.ActivityId == id).Select(t => t.Id).ToListAsync();
        var attrIds = await _db.ActivityAttributes.Where(a => a.ActivityId == id).Select(a => a.Id).ToListAsync();

        // Clear Restrict FKs first
        await _db.StudentAttributeValues.Where(v => attrIds.Contains(v.ActivityAttributeId)).ExecuteDeleteAsync();
        await _db.PresentationStudents.Where(ps => taskIds.Contains(ps.TaskItemId)).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => taskIds.Contains(e.TaskItemId)).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => a.ActivityId == id).ExecuteDeleteAsync();

        // Now safe to delete remaining children and the activity itself
        await _db.ActivityAttributeOptions.Where(o => attrIds.Contains(o.ActivityAttributeId)).ExecuteDeleteAsync();
        await _db.ActivityAttributes.Where(a => a.ActivityId == id).ExecuteDeleteAsync();
        await _db.TaskItems.Where(t => t.ActivityId == id).ExecuteDeleteAsync();
        await _db.Activities.Where(a => a.Id == id).ExecuteDeleteAsync();

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
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> BulkAssign([FromBody] int[]? activityIds)
    {
        if (activityIds == null || activityIds.Length == 0)
            return BadRequest("No activities selected.");
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
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> BulkDelete([FromBody] int[]? activityIds)
    {
        if (activityIds == null || activityIds.Length == 0)
            return Json(new { success = false, message = "No activities selected." });

        var idList = activityIds.ToList();
        var taskIds = await _db.TaskItems.Where(t => idList.Contains(t.ActivityId)).Select(t => t.Id).ToListAsync();
        var attrIds = await _db.ActivityAttributes.Where(a => idList.Contains(a.ActivityId)).Select(a => a.Id).ToListAsync();

        await _db.StudentAttributeValues.Where(v => attrIds.Contains(v.ActivityAttributeId)).ExecuteDeleteAsync();
        await _db.PresentationStudents.Where(ps => taskIds.Contains(ps.TaskItemId)).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => taskIds.Contains(e.TaskItemId)).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => idList.Contains(a.ActivityId)).ExecuteDeleteAsync();
        await _db.ActivityAttributeOptions.Where(o => attrIds.Contains(o.ActivityAttributeId)).ExecuteDeleteAsync();
        await _db.ActivityAttributes.Where(a => idList.Contains(a.ActivityId)).ExecuteDeleteAsync();
        await _db.TaskItems.Where(t => idList.Contains(t.ActivityId)).ExecuteDeleteAsync();
        await _db.Activities.Where(a => idList.Contains(a.Id)).ExecuteDeleteAsync();

        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> GetEligibleStudents(int activityId, bool includeAlreadyAssigned = false)
    {
        var activity = await _db.Activities
            .Include(a => a.Group).ThenInclude(g => g.Students)
            .FirstOrDefaultAsync(a => a.Id == activityId);

        if (activity == null) return NotFound();

        // Always exclude students already assigned to THIS activity
        var assignedToThis = await _db.Assignments
            .Where(a => a.ActivityId == activityId)
            .Select(a => a.StudentId)
            .ToListAsync();

        var pool = activity.Group.Students
            .Where(s => s.IsActive && !assignedToThis.Contains(s.Id));

        if (!includeAlreadyAssigned)
        {
            // Also exclude students assigned to any other activity in this group
            var assignedToAny = await _db.Assignments
                .Where(a => a.Activity.GroupId == activity.GroupId)
                .Select(a => a.StudentId)
                .Distinct()
                .ToListAsync();

            pool = pool.Where(s => !assignedToAny.Contains(s.Id));
        }

        var eligible = pool
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new { s.Id, FullName = s.FirstName + " " + s.LastName })
            .ToList();

        return Json(eligible);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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

        var activity = await _db.Activities
            .Include(a => a.Group).ThenInclude(g => g.Students)
            .FirstOrDefaultAsync(a => a.Id == activityId);

        if (activity == null) return NotFound();

        var allStudentNames = activity.Group.Students
            .Where(s => s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => $"{s.FirstName} {s.LastName}")
            .ToList();

        var vm = new DrawResultVm
        {
            ActivityId = activityId,
            ActivityName = activity.Name,
            SourceName = activity.Name,
            DrawTypeName = "Aktivita",
            DrawnStudentNames = drawnNames,
            AllStudentNames = allStudentNames
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> DrawForPresentation(int taskId, int count)
    {
        try
        {
            var drawn = await _assignmentService.DrawAddForPresentationAsync(taskId, count);
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

        var task = await _db.TaskItems
            .Include(t => t.Activity)
                .ThenInclude(a => a.Group)
                    .ThenInclude(g => g.Students)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null) return NotFound();

        if (drawnJson == null)
            return RedirectToAction(nameof(Details), new { id = task.ActivityId });

        var drawnNames = JsonSerializer.Deserialize<List<string>>(drawnJson)!;

        var allStudentNames = task.Activity.Group.Students
            .Where(s => s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => $"{s.FirstName} {s.LastName}")
            .ToList();

        var vm = new DrawResultVm
        {
            ActivityId = task.ActivityId,
            ActivityName = task.Activity.Name,
            SourceName = task.Title,
            DrawTypeName = "Prezentácia",
            DrawnStudentNames = drawnNames,
            AllStudentNames = allStudentNames
        };

        return View("DrawResult", vm);
    }

    [HttpPost]
    public async Task<IActionResult> Duplicate(int id)
    {
        var source = await _db.Activities
            .Include(a => a.Tasks)
            .Include(a => a.Assignments)
            .Include(a => a.OtherAttributes).ThenInclude(attr => attr.Options)
            .Include(a => a.OtherAttributes).ThenInclude(attr => attr.StudentValues)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (source == null)
            return Json(new { success = false, message = "Activity not found." });

        var copy = new Activity
        {
            Name        = source.Name + " (copy)",
            Description = source.Description,
            GroupId     = source.GroupId,
            CreatedAt   = DateTime.UtcNow
        };

        foreach (var t in source.Tasks)
        {
            copy.Tasks.Add(new TaskItem
            {
                Title            = t.Title,
                IsPresentation   = t.IsPresentation,
                PresentationDate = t.PresentationDate
            });
        }

        foreach (var a in source.Assignments)
        {
            copy.Assignments.Add(new Assignment
            {
                StudentId  = a.StudentId,
                AssignedAt = DateTime.UtcNow
            });
        }

        // Build new attributes, keeping a map old option ID → new option so values can be remapped
        var optionIdMap = new Dictionary<int, ActivityAttributeOption>();
        foreach (var attr in source.OtherAttributes)
        {
            var attrCopy = new ActivityAttribute { Name = attr.Name };
            foreach (var opt in attr.Options)
            {
                var optCopy = new ActivityAttributeOption { Name = opt.Name, Color = opt.Color };
                attrCopy.Options.Add(optCopy);
                optionIdMap[opt.Id] = optCopy;
            }
            copy.OtherAttributes.Add(attrCopy);
        }

        _db.Activities.Add(copy);
        await _db.SaveChangesAsync(); // IDs for new attrs/options are now assigned

        // Copy attribute values, remapping option IDs to the new copies
        var attrMap = source.OtherAttributes
            .Zip(copy.OtherAttributes, (src, dst) => (src, dst))
            .ToDictionary(p => p.src.Id, p => p.dst);

        foreach (var attr in source.OtherAttributes)
        {
            if (!attrMap.TryGetValue(attr.Id, out var newAttr)) continue;
            foreach (var val in attr.StudentValues)
            {
                ActivityAttributeOption? newOpt = null;
                if (val.OptionId.HasValue)
                    optionIdMap.TryGetValue(val.OptionId.Value, out newOpt);

                _db.StudentAttributeValues.Add(new StudentAttributeValue
                {
                    StudentId           = val.StudentId,
                    ActivityAttributeId = newAttr.Id,
                    OptionId            = newOpt?.Id
                });
            }
        }

        await _db.SaveChangesAsync();

        return Json(new { success = true, newId = copy.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActivityAssignments(int activityId, int[]? studentIds)
    {
        var activityExists = await _db.Activities.AnyAsync(a => a.Id == activityId);
        if (!activityExists) return NotFound();

        var existing = _db.Assignments.Where(a => a.ActivityId == activityId);
        _db.Assignments.RemoveRange(existing);
        await _db.SaveChangesAsync();

        if (studentIds != null)
        {
            foreach (var studentId in studentIds)
            {
                _db.Assignments.Add(new Assignment { StudentId = studentId, ActivityId = activityId });
            }
            await _db.SaveChangesAsync();
        }

        return Ok();
    }
}
