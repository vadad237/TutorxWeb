using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;

namespace StudentApp.Web.Services;

public class TaskService : ITaskService
{
    private readonly AppDbContext _db;

    public TaskService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TaskItem> CreateTaskAsync(string title, int activityId, DateTime? presentationDate, bool isPresentation)
    {
        var task = new TaskItem
        {
            Title = title.Trim(),
            ActivityId = activityId,
            PresentationDate = presentationDate,
            IsPresentation = isPresentation
        };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<(bool Success, string? Message)> SetTitleAsync(int id, string title)
    {
        var task = await _db.TaskItems.FindAsync(id);
        if (task == null) return (false, "Task not found.");

        task.Title = title.Trim();
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Message)> SetDateAsync(int id, DateTime? presentationDate)
    {
        var task = await _db.TaskItems.FindAsync(id);
        if (task == null) return (false, "Task not found.");

        task.PresentationDate = presentationDate;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task SetPresentationStudentsAsync(int taskId, int[]? studentIds)
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
    }

    public async Task<(bool Found, int? ActivityId)> DeleteTaskAsync(int id)
    {
        var task = await _db.TaskItems.FindAsync(id);
        if (task == null) return (false, null);

        var activityId = task.ActivityId;

        await _db.PresentationStudents.Where(ps => ps.TaskItemId == id).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => e.TaskItemId == id).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => a.TaskItemId == id).ExecuteDeleteAsync();

        _db.TaskItems.Remove(task);
        await _db.SaveChangesAsync();

        return (true, activityId);
    }

    public async Task<List<EligibleStudentDto>?> GetEligiblePresentationStudentsAsync(int taskId, bool includeAlreadyAssigned)
    {
        var task = await _db.TaskItems
            .Include(t => t.Activity).ThenInclude(a => a.Assignments).ThenInclude(a => a.Student)
            .Include(t => t.PresentationStudents)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.IsPresentation);

        if (task == null) return null;

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

        return pool
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new EligibleStudentDto(s.Id, s.FirstName + " " + s.LastName))
            .ToList();
    }
}
