using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;

namespace StudentApp.Web.Controllers;

public class TasksController : Controller
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create(string title, int activityId, DateTime? presentationDate, bool isPresentation = false)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Json(new { success = false, message = "Title is required." });

        var task = new TaskItem
        {
            Title = title.Trim(),
            ActivityId = activityId,
            PresentationDate = presentationDate,
            IsPresentation = isPresentation
        };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        return Json(new { success = true, taskId = task.Id, title = task.Title });
    }

    [HttpPost]
    public async Task<IActionResult> SetTitle(int id, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Json(new { success = false, message = "Title required." });
        var task = await _db.TaskItems.FindAsync(id);
        if (task == null) return Json(new { success = false, message = "Task not found." });
        task.Title = title.Trim();
        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> SetDate(int id, DateTime? presentationDate)
    {
        var task = await _db.TaskItems.FindAsync(id);
        if (task == null)
            return Json(new { success = false, message = "Task not found." });

        task.PresentationDate = presentationDate;
        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPresentationStudents(int taskId, int[]? studentIds)
    {
        var existing = _db.PresentationStudents.Where(ps => ps.TaskItemId == taskId);
        _db.PresentationStudents.RemoveRange(existing);
        await _db.SaveChangesAsync();

        if (studentIds != null)
        {
            foreach (var sid in studentIds)
                _db.PresentationStudents.Add(new PresentationStudent { TaskItemId = taskId, StudentId = sid });
            await _db.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _db.TaskItems.FindAsync(id);
        if (task == null)
            return Json(new { success = false, message = "Task not found." });

        var activityId = task.ActivityId;

        // Clear Restrict FK children before removing the task
        await _db.PresentationStudents.Where(ps => ps.TaskItemId == id).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => e.TaskItemId == id).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => a.TaskItemId == id).ExecuteDeleteAsync();

        _db.TaskItems.Remove(task);
        await _db.SaveChangesAsync();

        return Json(new { success = true, activityId });
    }

    [HttpGet]
    public async Task<IActionResult> GetEligiblePresentationStudents(int taskId, bool includeAlreadyAssigned = false)
    {
        var task = await _db.TaskItems
            .Include(t => t.Activity).ThenInclude(a => a.Assignments).ThenInclude(a => a.Student)
            .Include(t => t.PresentationStudents)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.IsPresentation);

        if (task == null) return NotFound();

        var assignedToThis = task.PresentationStudents.Select(ps => ps.StudentId).ToHashSet();

        var pool = task.Activity.Assignments
            .Select(a => a.Student)
            .Where(s => s.IsActive && !assignedToThis.Contains(s.Id));

        if (!includeAlreadyAssigned)
        {
            var assignedToAnyPres = await _db.PresentationStudents
                .Where(ps => ps.TaskItem.ActivityId == task.ActivityId)
                .Select(ps => ps.StudentId)
                .Distinct()
                .ToListAsync();

            pool = pool.Where(s => !assignedToAnyPres.Contains(s.Id));
        }

        var eligible = pool
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new { s.Id, FullName = s.FirstName + " " + s.LastName })
            .ToList();

        return Json(eligible);
    }
}
