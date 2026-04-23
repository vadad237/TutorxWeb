using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.DTOs;
using StudentApp.Web.Models.Entities;

namespace StudentApp.Web.Services;

public class AssignmentService : IAssignmentService
{
    private readonly AppDbContext _db;

    public AssignmentService(AppDbContext db)
    {
        _db = db;
    }

    // Delegates to AssignStudentsAsync — kept for interface compatibility
    public Task<AssignmentResultDto> AssignTasksAsync(int activityId)
        => AssignStudentsAsync(activityId);

    // Bulk assign: shuffles ALL active students and activities once then distributes students evenly
    // across the given activities (round-robin). Within each activity students are
    // also spread evenly across that activity's tasks.
    public async Task BulkAssignAsync(int[] activityIds)
    {
        if (activityIds.Length == 0)
            return;

        var ids = activityIds.ToList();

        var activities = await _db.Activities
            .Where(a => ids.Contains(a.Id))
            .Include(a => a.Group).ThenInclude(g => g.Students)
            .Include(a => a.Tasks)
            .ToListAsync();

        if (activities.Count == 0)
            return;

        activities = activities.OrderBy(_ => Random.Shared.Next()).ToList();

        var activeStudents = activities[0].Group.Students
            .Where(s => s.IsActive)
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        var buckets = new List<List<int>>(activities.Count);
        for (int i = 0; i < activities.Count; i++)
            buckets.Add([]);

        for (int i = 0; i < activeStudents.Count; i++)
            buckets[i % activities.Count].Add(activeStudents[i].Id);

        var taskIds = await _db.TaskItems
            .Where(t => ids.Contains(t.ActivityId))
            .Select(t => t.Id)
            .ToListAsync();

        var existingPresStudents = _db.PresentationStudents.Where(ps => taskIds.Contains(ps.TaskItemId));
        _db.PresentationStudents.RemoveRange(existingPresStudents);

        var existing = _db.Assignments.Where(a => ids.Contains(a.ActivityId));
        _db.Assignments.RemoveRange(existing);
        await _db.SaveChangesAsync();

        var newAssignments = new List<Assignment>();
        for (int ai = 0; ai < activities.Count; ai++)
        {
            var activity = activities[ai];
            var tasks = activity.Tasks.ToList();
            var studentIds = buckets[ai];

            for (int si = 0; si < studentIds.Count; si++)
            {
                newAssignments.Add(new Assignment
                {
                    StudentId = studentIds[si],
                    ActivityId = activity.Id,
                    TaskItemId = tasks.Count > 0 ? tasks[si % tasks.Count].Id : null
                });
            }
        }

        _db.Assignments.AddRange(newAssignments);
        await _db.SaveChangesAsync();
    }

    // Auto-assign: randomly and evenly distributes ALL active students across tasks.
    // Students are shuffled then assigned round-robin so each task gets as equal a
    // share as possible. If there are no tasks, students are assigned without a task.
    public async Task<AssignmentResultDto> AssignStudentsAsync(int activityId)
    {
        var activity = await _db.Activities
            .Include(a => a.Group).ThenInclude(g => g.Students)
            .Include(a => a.Tasks)
            .FirstOrDefaultAsync(a => a.Id == activityId)
            ?? throw new InvalidOperationException("Aktivita nebola nájdená.");

        var activeStudents = activity.Group.Students
            .Where(s => s.IsActive)
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        var tasks = activity.Tasks.ToList();

        var taskIds = await _db.TaskItems
            .Where(t => t.ActivityId == activityId)
            .Select(t => t.Id)
            .ToListAsync();

        var existingPresStudents = _db.PresentationStudents.Where(ps => taskIds.Contains(ps.TaskItemId));
        _db.PresentationStudents.RemoveRange(existingPresStudents);

        var existing = _db.Assignments.Where(a => a.ActivityId == activityId);
        _db.Assignments.RemoveRange(existing);
        await _db.SaveChangesAsync();

        var assignments = new List<Assignment>(activeStudents.Count);
        for (int i = 0; i < activeStudents.Count; i++)
        {
            assignments.Add(new Assignment
            {
                StudentId = activeStudents[i].Id,
                ActivityId = activityId,
                TaskItemId = tasks.Count > 0 ? tasks[i % tasks.Count].Id : null
            });
        }

        _db.Assignments.AddRange(assignments);
        await _db.SaveChangesAsync();
        return new AssignmentResultDto(assignments, activeStudents.Count, tasks.Count);
    }

    // Draw N random students for the activity, replacing existing assignments
    public async Task<List<Student>> DrawForActivityAsync(int activityId, int count)
    {
        var activity = await _db.Activities
            .Include(a => a.Group).ThenInclude(g => g.Students)
            .FirstOrDefaultAsync(a => a.Id == activityId)
            ?? throw new InvalidOperationException("Aktivita nebola nájdená.");

        var activeStudents = activity.Group.Students
            .Where(s => s.IsActive).ToList();

        var drawCount = Math.Min(count, activeStudents.Count);
        var drawn = activeStudents.OrderBy(_ => Random.Shared.Next()).Take(drawCount).ToList();

        var existing = _db.Assignments.Where(a => a.ActivityId == activityId);
        _db.Assignments.RemoveRange(existing);

        foreach (var student in drawn)
        {
            _db.Assignments.Add(new Assignment
            {
                StudentId = student.Id,
                ActivityId = activityId
            });
        }

        await _db.SaveChangesAsync();
        return drawn;
    }

    // Draw N random students for the activity, ADDING to existing (no reset)
    public async Task<List<Student>> DrawAddForActivityAsync(int activityId, int count, bool includeAlreadyAssigned = false, List<int>? allowedStudentIds = null)
    {
        var activity = await _db.Activities
            .Include(a => a.Group).ThenInclude(g => g.Students)
            .FirstOrDefaultAsync(a => a.Id == activityId)
            ?? throw new InvalidOperationException("Aktivita nebola nájdená.");

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

        // Restrict to the client-side selection when provided
        if (allowedStudentIds != null && allowedStudentIds.Count > 0)
            pool = pool.Where(s => allowedStudentIds.Contains(s.Id));

        var eligible = pool.ToList();

        var drawCount = Math.Min(count, eligible.Count);
        var drawn = eligible.OrderBy(_ => Random.Shared.Next()).Take(drawCount).ToList();

        // Determine the current draw cycle for this group
        var currentCycle = await _db.DrawHistories
            .Where(d => d.GroupId == activity.GroupId)
            .MaxAsync(d => (int?)d.CycleNumber) ?? 1;

        // Use a single shared timestamp so all students in this batch are grouped together
        var drawnAt = DateTime.UtcNow;

        foreach (var student in drawn)
        {
            _db.Assignments.Add(new Assignment
            {
                StudentId = student.Id,
                ActivityId = activityId
            });

            _db.DrawHistories.Add(new DrawHistory
            {
                StudentId = student.Id,
                GroupId = activity.GroupId,
                ActivityId = activityId,
                CycleNumber = currentCycle,
                DrawnAt = drawnAt
            });
        }

        await _db.SaveChangesAsync();
        return drawn;
    }

    // Draw N random students for a presentation, ADDING to existing PresentationStudents
    public async Task<List<Student>> DrawAddForPresentationAsync(int taskId, int count, PresentationRole role, bool includeAlreadyAssigned = false, List<int>? allowedStudentIds = null)
    {
        var task = await _db.TaskItems
            .Include(t => t.Activity).ThenInclude(a => a.Assignments).ThenInclude(a => a.Student)
            .Include(t => t.PresentationStudents)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.IsPresentation)
            ?? throw new InvalidOperationException("Prezentácia nebola nájdená.");

        // Exclude ALL students already assigned to this presentation in any role:
        // - same role: can't draw twice
        // - opposite role: can't be both prezentujúci and náhradník on the same presentation
        var assignedToThisPres = task.PresentationStudents
            .Select(ps => ps.StudentId).ToHashSet();

        // Pool = students assigned to the parent activity, minus anyone already on this presentation
        var pool = task.Activity.Assignments
            .Select(a => a.Student)
            .Where(s => s.IsActive && !assignedToThisPres.Contains(s.Id));

        if (!includeAlreadyAssigned)
        {
            // Exclude students already assigned to this same role in any other presentation in this activity
            var assignedToAnyPres = await _db.PresentationStudents
                .Where(ps => ps.TaskItem.ActivityId == task.ActivityId && ps.Role == role)
                .Select(ps => ps.StudentId)
                .Distinct()
                .ToListAsync();

            pool = pool.Where(s => !assignedToAnyPres.Contains(s.Id));
        }

        // Restrict to the client-side selection when provided
        if (allowedStudentIds != null && allowedStudentIds.Count > 0)
            pool = pool.Where(s => allowedStudentIds.Contains(s.Id));

        var eligible = pool.ToList();
        var drawCount = Math.Min(count, eligible.Count);
        var drawn = eligible.OrderBy(_ => Random.Shared.Next()).Take(drawCount).ToList();

        // Use a single shared timestamp so all students in this batch are grouped together
        var drawnAt = DateTime.UtcNow;

        // Determine the current draw cycle for this group
        var currentCycle = await _db.DrawHistories
            .Where(d => d.GroupId == task.Activity.GroupId)
            .MaxAsync(d => (int?)d.CycleNumber) ?? 1;

        foreach (var student in drawn)
        {
            _db.PresentationStudents.Add(new PresentationStudent
            {
                TaskItemId = taskId,
                StudentId = student.Id,
                Role = role
            });

            _db.DrawHistories.Add(new DrawHistory
            {
                StudentId = student.Id,
                GroupId = task.Activity.GroupId,
                ActivityId = task.ActivityId,
                TaskItemId = taskId,
                CycleNumber = currentCycle,
                DrawnAt = drawnAt,
                Role = role
            });
        }

        await _db.SaveChangesAsync();
        return drawn;
    }
}
