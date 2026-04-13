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

    public async Task SetPresentationStudentsByRoleAsync(int taskId, int[]? studentIds, PresentationRole role)
    {
        var existing = _db.PresentationStudents.Where(ps => ps.TaskItemId == taskId && ps.Role == role);
        _db.PresentationStudents.RemoveRange(existing);
        await _db.SaveChangesAsync();

        if (studentIds != null)
        {
            foreach (var sid in studentIds)
                _db.PresentationStudents.Add(new PresentationStudent { TaskItemId = taskId, StudentId = sid, Role = role });
            await _db.SaveChangesAsync();
        }
    }

    public async Task<(bool Found, int? ActivityId)> DeleteTaskAsync(int id)
    {
        var activityId = await _db.TaskItems
            .Where(t => t.Id == id)
            .Select(t => (int?)t.ActivityId)
            .FirstOrDefaultAsync();

        if (activityId == null) return (false, null);

        await _db.PresentationStudents.Where(ps => ps.TaskItemId == id).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => e.TaskItemId == id).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => a.TaskItemId == id).ExecuteDeleteAsync();
        await _db.DrawHistories.Where(d => d.TaskItemId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.TaskItemId, (int?)null));
        await _db.TaskItems.Where(t => t.Id == id).ExecuteDeleteAsync();

        return (true, activityId);
    }

    public async Task<List<EligibleStudentDto>?> GetEligiblePresentationStudentsAsync(int taskId, bool includeAlreadyAssigned, PresentationRole? role = null)
    {
        var task = await _db.TaskItems
            .Include(t => t.Activity).ThenInclude(a => a.Assignments).ThenInclude(a => a.Student)
            .Include(t => t.PresentationStudents)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.IsPresentation);

        if (task == null) return null;

        // Exclude students already on this presentation in any role that conflicts:
        // - same role: can't draw twice for the same role
        // - opposite role: can't be both prezentujúci and náhradník on the same presentation
        var excludedFromThis = role.HasValue
            ? task.PresentationStudents.Select(ps => ps.StudentId).ToHashSet()
            : task.PresentationStudents.Select(ps => ps.StudentId).ToHashSet();

        var pool = task.Activity.Assignments
            .Select(a => a.Student)
            .Where(s => s.IsActive && !excludedFromThis.Contains(s.Id));

        if (!includeAlreadyAssigned)
        {
            var query = _db.PresentationStudents
                .Where(ps => ps.TaskItem.ActivityId == task.ActivityId);
            if (role.HasValue)
                query = query.Where(ps => ps.Role == role.Value);

            var assignedToAnyPres = await query
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
