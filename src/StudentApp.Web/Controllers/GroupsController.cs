using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;
using StudentApp.Web.Models.ViewModels;

namespace StudentApp.Web.Controllers;

public class GroupsController : Controller
{
    private readonly AppDbContext _db;

    public GroupsController(AppDbContext db)
    {
        _db = db;
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

    public async Task<IActionResult> Index(bool showArchived = false)
    {
        await PopulateActiveGroupAsync();
        var query = _db.Groups.AsQueryable();
        if (!showArchived)
            query = query.Where(g => !g.IsArchived);

        var groups = await query
            .Include(g => g.Students)
            .Select(g => new GroupSummaryVm
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                StudentCount = g.Students.Count,
                ActiveStudentCount = g.Students.Count(s => s.IsActive),
                IsArchived = g.IsArchived,
                CreatedAt = g.CreatedAt
            })
            .OrderBy(g => g.Name)
            .ToListAsync();

        ViewBag.ShowArchived = showArchived;
        return View(groups);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateActiveGroupAsync();
        return View(new GroupCreateVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GroupCreateVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        if (await _db.Groups.AnyAsync(g => g.Name == vm.Name.Trim()))
        {
            ModelState.AddModelError("Name", "A group with this name already exists.");
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        var group = new Group
        {
            Name = vm.Name.Trim(),
            Description = vm.Description?.Trim()
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Skupina '{group.Name}' bola úspešne vytvorená.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        await PopulateActiveGroupAsync();
        var group = await _db.Groups.FindAsync(id);
        if (group == null) return NotFound();

        return View(new GroupEditVm
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GroupEditVm vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        var group = await _db.Groups.FindAsync(id);
        if (group == null) return NotFound();

        if (await _db.Groups.AnyAsync(g => g.Name == vm.Name.Trim() && g.Id != id))
        {
            ModelState.AddModelError("Name", "A group with this name already exists.");
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        group.Name = vm.Name.Trim();
        group.Description = vm.Description?.Trim();
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Skupina '{group.Name}' bola úspešne aktualizovaná.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        await PopulateActiveGroupAsync();
        var group = await _db.Groups
            .Include(g => g.Students)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null) return NotFound();

        var vm = new GroupDetailsVm
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            TotalStudents = group.Students.Count,
            ActiveStudents = group.Students.Count(s => s.IsActive),
            InactiveStudents = group.Students.Count(s => !s.IsActive),
            CreatedAt = group.CreatedAt,
            Students = group.Students.Select(s => new StudentSummaryVm
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                IsActive = s.IsActive,
                GroupId = s.GroupId
            }).OrderBy(s => s.FullName).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var exists = await _db.Groups.AnyAsync(g => g.Id == id);
        if (!exists)
            return Json(new { success = false, message = "Group not found." });

        // Collect IDs needed for subqueries
        var studentIds  = _db.Students.Where(s => s.GroupId == id).Select(s => s.Id);
        var activityIds = _db.Activities.Where(a => a.GroupId == id).Select(a => a.Id);
        var taskIds     = _db.TaskItems.Where(t => activityIds.Contains(t.ActivityId)).Select(t => t.Id);
        var attrIds     = _db.ActivityAttributes.Where(a => activityIds.Contains(a.ActivityId)).Select(a => a.Id);

        // Delete in strict FK dependency order (Restrict constraints must be cleared first)
        await _db.StudentAttributeValues.Where(v => studentIds.Contains(v.StudentId) || attrIds.Contains(v.ActivityAttributeId)).ExecuteDeleteAsync();
        await _db.PresentationStudents.Where(ps => studentIds.Contains(ps.StudentId) || taskIds.Contains(ps.TaskItemId)).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => studentIds.Contains(e.StudentId)).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => studentIds.Contains(a.StudentId)).ExecuteDeleteAsync();
        await _db.DrawHistories.Where(d => studentIds.Contains(d.StudentId)).ExecuteDeleteAsync();
        await _db.Attendances.Where(a => studentIds.Contains(a.StudentId) || a.GroupId == id).ExecuteDeleteAsync();
        await _db.Students.Where(s => s.GroupId == id).ExecuteDeleteAsync();

        // ActivityAttributeOptions cascade from ActivityAttribute; both cascade from Activity
        await _db.ActivityAttributeOptions.Where(o => attrIds.Contains(o.ActivityAttributeId)).ExecuteDeleteAsync();
        await _db.ActivityAttributes.Where(a => activityIds.Contains(a.ActivityId)).ExecuteDeleteAsync();
        await _db.TaskItems.Where(t => activityIds.Contains(t.ActivityId)).ExecuteDeleteAsync();
        await _db.Activities.Where(a => a.GroupId == id).ExecuteDeleteAsync();

        await _db.DrawHistories.Where(d => d.GroupId == id).ExecuteDeleteAsync();
        await _db.Groups.Where(g => g.Id == id).ExecuteDeleteAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Archive(int id)
    {
        var group = await _db.Groups
            .Include(g => g.Activities).ThenInclude(a => a.Tasks)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
            return Json(new { success = false, message = "Group not found." });

        // Check for activities with future-dated tasks
        if (group.Activities.Any(a => !a.IsArchived && a.Tasks.Any(t => t.PresentationDate > DateTime.UtcNow)))
            return Json(new { success = false, message = "Cannot archive group with future-dated activities." });

        group.IsArchived = true;
        await _db.SaveChangesAsync();

        return Json(new { success = true, message = $"Group '{group.Name}' archived." });
    }

    [HttpPost]
    public async Task<IActionResult> Unarchive(int id)
    {
        var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == id);
        if (group == null)
            return Json(new { success = false, message = "Group not found." });

        group.IsArchived = false;
        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id)
    {
        var group = await _db.Groups.FindAsync(id);
        if (group == null) return NotFound();

        HttpContext.Session.SetActiveGroup(id);
        return Redirect(Request.Headers.Referer.ToString() ?? Url.Action("Index")!);
    }
}
