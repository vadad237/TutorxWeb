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

    public async Task<TaskItem> CreateTaskAsync(string title, int activityId, DateTime? presentationDate, bool isPresentation, decimal? maxScore = null)
    {
        var task = new TaskItem
        {
            Title = title.Trim(),
            ActivityId = activityId,
            PresentationDate = presentationDate,
            IsPresentation = isPresentation,
            MaxScore = maxScore,
            CreatedAt = DateTime.UtcNow
        };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<List<TaskItem>> CreateNumberedTasksAsync(int activityId, int count)
    {
        var existing = await _db.TaskItems
            .Where(t => t.ActivityId == activityId && t.IsNumberedTask)
            .Select(t => t.Title)
            .ToListAsync();

        var existingNumbers = existing
            .Select(t => int.TryParse(t, out var n) ? n : 0)
            .ToHashSet();

        var nextNumber = (existingNumbers.Count > 0 ? existingNumbers.Max() : 0) + 1;
        var created = new List<TaskItem>();

        for (int i = 0; i < count; i++)
        {
            var task = new TaskItem
            {
                Title = (nextNumber + i).ToString(),
                ActivityId = activityId,
                IsNumberedTask = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.TaskItems.Add(task);
            created.Add(task);
        }

        await _db.SaveChangesAsync();
        return created;
    }

    public async Task<(bool Success, string? Message)> SetTitleAsync(int id, string title)
    {
        var task = await _db.TaskItems.FindAsync(id);
        if (task == null)
            return (false, "Úloha nebola nájdená.");

        task.Title = title.Trim();
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Message)> SetDateAsync(int id, DateTime? presentationDate)
    {
        var task = await _db.TaskItems.FindAsync(id);
        if (task == null)
            return (false, "Úloha nebola nájdená.");

        task.PresentationDate = presentationDate;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Message)> SetMaxScoreAsync(int id, decimal? maxScore)
    {
        var task = await _db.TaskItems.FindAsync(id);
        if (task == null)
            return (false, "Úloha nebola nájdená.");

        task.MaxScore = maxScore.HasValue ? Math.Round(maxScore.Value, 2) : null;
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

        if (activityId == null)
            return (false, null);

        await _db.PresentationStudents.Where(ps => ps.TaskItemId == id).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => e.TaskItemId == id).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => a.TaskItemId == id).ExecuteDeleteAsync();
        await _db.DrawHistories.Where(d => d.TaskItemId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.TaskItemId, (int?)null));
        await _db.TaskItems.Where(t => t.Id == id).ExecuteDeleteAsync();

        return (true, activityId);
    }

    public async Task BulkDeleteTasksAsync(int[] ids)
    {
        if (ids == null || ids.Length == 0)
            return;
        var idSet = ids.ToHashSet();
        await _db.PresentationStudents.Where(ps => idSet.Contains(ps.TaskItemId)).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => idSet.Contains(e.TaskItemId)).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => a.TaskItemId.HasValue && idSet.Contains(a.TaskItemId.Value)).ExecuteDeleteAsync();
        await _db.DrawHistories.Where(d => d.TaskItemId != null && idSet.Contains(d.TaskItemId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.TaskItemId, (int?)null));
        await _db.TaskItems.Where(t => idSet.Contains(t.Id)).ExecuteDeleteAsync();
    }

    public async Task<List<EligibleStudentDto>?> GetEligiblePresentationStudentsAsync(int taskId, bool includeAlreadyAssigned, PresentationRole? role = null)
    {
        var task = await _db.TaskItems
            .Include(t => t.Activity).ThenInclude(a => a.Assignments).ThenInclude(a => a.Student)
            .Include(t => t.PresentationStudents)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.IsPresentation);

        if (task == null)
            return null;

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
                .Where(ps => ps.TaskItem.ActivityId == task.ActivityId && ps.TaskItem.IsPresentation);
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

    // Auto-assign: shuffles the given numbered tasks, shuffles the activity's assigned students,
    // then assigns students round-robin across tasks. Wraps around until every student has exactly
    // one task. Clears existing PresentationStudent (role=Presentee) records for those tasks first.
    public async Task<(bool Success, string Message)> AutoAssignNumberedTasksAsync(int activityId, int[] taskIds)
    {
        if (taskIds.Length == 0)
            return (false, "Žiadne zadania neboli vybrané.");

        var taskIdList = taskIds.ToList();

        var tasks = await _db.TaskItems
            .Where(t => t.ActivityId == activityId && t.IsNumberedTask && taskIdList.Contains(t.Id))
            .ToListAsync();

        if (tasks.Count == 0)
            return (false, "Žiadne platné zadania neboli nájdené.");

        var studentIds = await _db.Assignments
            .Where(a => a.ActivityId == activityId)
            .Select(a => a.StudentId)
            .Distinct()
            .ToListAsync();

        if (studentIds.Count == 0)
            return (false, "K tejto aktivite nie sú priradení žiadni študenti.");

        // Shuffle both lists
        tasks = tasks.OrderBy(_ => Random.Shared.Next()).ToList();
        studentIds = studentIds.OrderBy(_ => Random.Shared.Next()).ToList();

        // Clear existing Presentee assignments for the selected tasks
        var taskIdSet = tasks.Select(t => t.Id).ToList();
        var toRemove = await _db.PresentationStudents
            .Where(ps => taskIdSet.Contains(ps.TaskItemId) && ps.Role == PresentationRole.Presentee)
            .ToListAsync();
        _db.PresentationStudents.RemoveRange(toRemove);
        await _db.SaveChangesAsync();

        // Assign each student to a task, wrapping around if there are more students than tasks
        var newAssignments = new List<PresentationStudent>(studentIds.Count);
        for (int i = 0; i < studentIds.Count; i++)
        {
            newAssignments.Add(new PresentationStudent
            {
                TaskItemId = tasks[i % tasks.Count].Id,
                StudentId = studentIds[i],
                Role = PresentationRole.Presentee
            });
        }

        _db.PresentationStudents.AddRange(newAssignments);
        await _db.SaveChangesAsync();

        return (true, $"Priradených {studentIds.Count} študentov k {tasks.Count} zadaniam.");
    }
}
