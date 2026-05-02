using Microsoft.EntityFrameworkCore;
using Tutorx.Web.Data;
using Tutorx.Web.Models.Entities;
using Tutorx.Web.Models.ViewModels;

namespace Tutorx.Web.Services;

public class ActivityService : IActivityService
{
    private readonly AppDbContext _db;

    public ActivityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ActivitySummaryVm>> GetActivitySummariesAsync(int groupId)
    {
        var projected = await _db.Activities
            .Where(a => !a.IsArchived && a.GroupId == groupId)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.Name,
                GroupName = a.Group.Name,
                a.GroupId,
                a.IsArchived,
                NumberedTaskCount = a.Tasks.Count(t => t.IsNumberedTask),
                OrdinaryTaskCount = a.Tasks.Count(t => !t.IsNumberedTask && !t.IsPresentation),
                PresentationCount = a.Tasks.Count(t => t.IsPresentation),
                AssignedCount = a.Assignments.Select(x => x.StudentId).Distinct().Count(),
                AssignedStudentDetails = a.Assignments
                    .Select(asgn => new
                    {
                        asgn.StudentId,
                        Name = asgn.Student.FirstName + " " + asgn.Student.LastName
                    })
                    .ToList(),
                StudentsWithNumberedTask = a.Tasks
                    .Where(t => t.IsNumberedTask)
                    .SelectMany(t => t.PresentationStudents.Select(ps => ps.StudentId))
                    .ToList()
            })
            .ToListAsync();

        return projected.Select(a =>
        {
            var distinctStudents = a.AssignedStudentDetails
                .DistinctBy(x => x.StudentId)
                .OrderBy(x => x.Name)
                .ToList();

            return new ActivitySummaryVm
            {
                Id = a.Id,
                Name = a.Name,
                GroupName = a.GroupName,
                GroupId = a.GroupId,
                IsArchived = a.IsArchived,
                NumberedTaskCount = a.NumberedTaskCount,
                OrdinaryTaskCount = a.OrdinaryTaskCount,
                PresentationCount = a.PresentationCount,
                AssignedCount = a.AssignedCount,
                AssignedStudents = distinctStudents.Select(x => x.Name).ToList(),
                AssignedStudentDetails = distinctStudents.Select(x => (x.StudentId, x.Name)).ToList(),
                StudentsWithNumberedTask = a.StudentsWithNumberedTask.ToHashSet()
            };
        }).ToList();
    }

    public async Task<(int Id, string Name)?> GetGroupInfoAsync(int groupId)
    {
        var group = await _db.Groups.Where(g => g.Id == groupId && !g.IsArchived)
            .Select(g => new { g.Id, g.Name }).FirstOrDefaultAsync();
        if (group == null)
            return null;
        return (group.Id, group.Name);
    }

    public async Task<Activity> CreateActivityAsync(string name, string? description, int groupId)
    {
        var activity = new Activity
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            GroupId = groupId
        };
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    public async Task<Activity?> GetActivityByIdAsync(int id)
    {
        return await _db.Activities.FindAsync(id);
    }

    public async Task<Activity?> UpdateActivityAsync(int id, string name, string? description)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity == null)
            return null;

        activity.Name = name.Trim();
        activity.Description = description?.Trim();
        await _db.SaveChangesAsync();
        return activity;
    }

    public async Task<(ActivityDetailsVm Details, List<StudentSummaryVm> AllStudents)?> GetActivityDetailsAsync(int id)
    {
        var activity = await _db.Activities
            .Include(a => a.Group).ThenInclude(g => g.Students)
            .Include(a => a.Tasks).ThenInclude(t => t.PresentationStudents).ThenInclude(ps => ps.Student)
            .Include(a => a.Assignments).ThenInclude(a => a.Student)
            .Include(a => a.OtherAttributes).ThenInclude(attr => attr.Options)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (activity == null)
            return null;

        var activeStudentCount = activity.Group.Students.Count(s => s.IsActive);
        var assignedStudentCount = activity.Assignments.Select(a => a.StudentId).Distinct().Count();

        var details = new ActivityDetailsVm
        {
            Id = activity.Id,
            Name = activity.Name,
            Description = activity.Description,
            GroupId = activity.GroupId,
            GroupName = activity.Group.Name,
            IsArchived = activity.IsArchived,
            Tasks = activity.Tasks
                .Where(t => !t.IsPresentation && !t.IsNumberedTask)
                .OrderBy(t => t.CreatedAt)
                .Select(t => new SimpleTaskVm(t.Id, t.Title, t.CreatedAt, t.MaxScore))
                .ToList(),
            NumberedTasks = activity.Tasks
                .Where(t => t.IsNumberedTask)
                .OrderBy(t => t.Title.Length).ThenBy(t => t.Title)
                .Select((t, i) => new NumberedTaskVm(
                    t.Id,
                    int.TryParse(t.Title, out var n) ? n : i + 1,
                    t.PresentationStudents
                        .Where(ps => ps.Role == PresentationRole.Presentee)
                        .Select(ps => new PresentationStudentVm(ps.StudentId, ps.Student.FullName))
                        .OrderBy(ps => ps.FullName)
                        .ToList()
                ))
                .ToList(),
            Presentations = activity.Tasks
                .Where(t => t.IsPresentation)
                .OrderBy(t => t.CreatedAt)
                .Select(t => new TaskWithAssignmentVm(
                    t.Id,
                    t.Title,
                    t.PresentationDate,
                        t.PresentationStudents
                        .Where(ps => ps.Role == PresentationRole.Presentee)
                        .Select(ps => new PresentationStudentVm(ps.StudentId, ps.Student.FullName))
                        .OrderBy(ps => ps.FullName)
                        .ToList(),
                    t.PresentationStudents
                        .Where(ps => ps.Role == PresentationRole.Substitution)
                        .Select(ps => new PresentationStudentVm(ps.StudentId, ps.Student.FullName))
                        .OrderBy(ps => ps.FullName)
                        .ToList(),
                    t.MaxScore
                )).ToList(),
            AssignedStudents = activity.Assignments.Select(a => new AssignedStudentVm(
                a.Id,
                a.StudentId,
                a.Student.FullName
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
        var attrIds = details.OtherAttributes.Select(a => a.AttributeId).ToList();
        var studentIds = details.AssignedStudents.Select(s => s.StudentId).ToList();
        if (attrIds.Any() && studentIds.Any())
        {
            var values = await _db.StudentAttributeValues
                .Include(v => v.Option)
                .Where(v => attrIds.Contains(v.ActivityAttributeId) && studentIds.Contains(v.StudentId))
                .ToListAsync();
            details.AttributeValues = values.Select(v => new StudentAttributeValueVm(
                v.StudentId, v.ActivityAttributeId, v.OptionId, v.Option?.Name, v.Option?.Color
            )).ToList();
        }

        var allStudents = activity.Group.Students
            .Where(s => s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new StudentSummaryVm { Id = s.Id, FullName = s.FullName })
            .ToList();

        return (details, allStudents);
    }

    public async Task<(bool Success, string Message)> ArchiveActivityAsync(int id)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity == null)
            return (false, "Aktivita nebola nájdená.");

        activity.IsArchived = true;
        await _db.SaveChangesAsync();
        return (true, $"Aktivita '{activity.Name}' bola archivovaná.");
    }

    public async Task<(bool Exists, bool Deleted)> DeleteActivityAsync(int id)
    {
        var exists = await _db.Activities.AnyAsync(a => a.Id == id);
        if (!exists)
            return (false, false);

        var taskIds = await _db.TaskItems.Where(t => t.ActivityId == id).Select(t => t.Id).ToListAsync();
        var attrIds = await _db.ActivityAttributes.Where(a => a.ActivityId == id).Select(a => a.Id).ToListAsync();

        await _db.StudentAttributeValues.Where(v => attrIds.Contains(v.ActivityAttributeId)).ExecuteDeleteAsync();
        await _db.PresentationStudents.Where(ps => taskIds.Contains(ps.TaskItemId)).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => taskIds.Contains(e.TaskItemId)).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => a.ActivityId == id).ExecuteDeleteAsync();

        await _db.ActivityAttributeOptions.Where(o => attrIds.Contains(o.ActivityAttributeId)).ExecuteDeleteAsync();
        await _db.ActivityAttributes.Where(a => a.ActivityId == id).ExecuteDeleteAsync();
        await _db.TaskItems.Where(t => t.ActivityId == id).ExecuteDeleteAsync();
        await _db.Activities.Where(a => a.Id == id).ExecuteDeleteAsync();

        return (true, true);
    }

    public async Task BulkDeleteActivitiesAsync(List<int> ids)
    {
        var taskIds = await _db.TaskItems.Where(t => ids.Contains(t.ActivityId)).Select(t => t.Id).ToListAsync();
        var attrIds = await _db.ActivityAttributes.Where(a => ids.Contains(a.ActivityId)).Select(a => a.Id).ToListAsync();

        await _db.StudentAttributeValues.Where(v => attrIds.Contains(v.ActivityAttributeId)).ExecuteDeleteAsync();
        await _db.PresentationStudents.Where(ps => taskIds.Contains(ps.TaskItemId)).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => taskIds.Contains(e.TaskItemId)).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => ids.Contains(a.ActivityId)).ExecuteDeleteAsync();
        await _db.ActivityAttributeOptions.Where(o => attrIds.Contains(o.ActivityAttributeId)).ExecuteDeleteAsync();
        await _db.ActivityAttributes.Where(a => ids.Contains(a.ActivityId)).ExecuteDeleteAsync();
        await _db.TaskItems.Where(t => ids.Contains(t.ActivityId)).ExecuteDeleteAsync();
        await _db.Activities.Where(a => ids.Contains(a.Id)).ExecuteDeleteAsync();
    }

    public async Task<List<EligibleStudentDto>?> GetEligibleStudentsAsync(int activityId, bool includeAlreadyAssigned)
    {
        var activity = await _db.Activities
            .Include(a => a.Group).ThenInclude(g => g.Students)
            .FirstOrDefaultAsync(a => a.Id == activityId);

        if (activity == null)
            return null;

        var assignedToThis = await _db.Assignments
            .Where(a => a.ActivityId == activityId)
            .Select(a => a.StudentId)
            .ToListAsync();

        var pool = activity.Group.Students
            .Where(s => s.IsActive && !assignedToThis.Contains(s.Id));

        if (!includeAlreadyAssigned)
        {
            var assignedToAny = await _db.Assignments
                .Where(a => a.Activity.GroupId == activity.GroupId)
                .Select(a => a.StudentId)
                .Distinct()
                .ToListAsync();

            pool = pool.Where(s => !assignedToAny.Contains(s.Id));
        }

        return pool
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new EligibleStudentDto(s.Id, s.FirstName + " " + s.LastName))
            .ToList();
    }

    public async Task<(bool Success, int? NewId, string? Message)> DuplicateActivityAsync(int id)
    {
        var source = await _db.Activities
            .Include(a => a.Tasks.Where(t => !t.IsNumberedTask))
            .Include(a => a.OtherAttributes).ThenInclude(attr => attr.Options)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (source == null)
            return (false, null, "Aktivita nebola nájdená.");

        var copy = new Activity
        {
            Name = source.Name + " (copy)",
            Description = source.Description,
            GroupId = source.GroupId,
            CreatedAt = DateTime.UtcNow
        };

        var taskIdMap = new Dictionary<int, TaskItem>();
        foreach (var t in source.Tasks)
        {
            var taskCopy = new TaskItem
            {
                Title = t.Title,
                IsPresentation = t.IsPresentation,
                IsNumberedTask = t.IsNumberedTask,
                PresentationDate = t.PresentationDate,
                MaxScore = t.MaxScore
            };
            copy.Tasks.Add(taskCopy);
            taskIdMap[t.Id] = taskCopy;
        }

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
        await _db.SaveChangesAsync();

        return (true, copy.Id, null);
    }

    public async Task<bool> SetActivityAssignmentsAsync(int activityId, int[]? studentIds)
    {
        var activityExists = await _db.Activities.AnyAsync(a => a.Id == activityId);
        if (!activityExists)
            return false;

        var keepIds = studentIds?.ToHashSet() ?? [];

        // Find students being removed so we can clean up their task assignments
        var removedStudentIds = await _db.Assignments
            .Where(a => a.ActivityId == activityId && !keepIds.Contains(a.StudentId))
            .Select(a => a.StudentId)
            .Distinct()
            .ToListAsync();

        if (removedStudentIds.Count > 0)
        {
            // Remove from presentations and numbered tasks belonging to this activity
            var stalePresStudents = await _db.PresentationStudents
                .Where(ps => ps.TaskItem.ActivityId == activityId
                          && removedStudentIds.Contains(ps.StudentId))
                .ToListAsync();

            _db.PresentationStudents.RemoveRange(stalePresStudents);
        }

        var existing = _db.Assignments.Where(a => a.ActivityId == activityId);
        _db.Assignments.RemoveRange(existing);

        if (studentIds != null)
        {
            foreach (var studentId in studentIds)
            {
                _db.Assignments.Add(new Assignment { StudentId = studentId, ActivityId = activityId });
            }
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<DrawActivityVm>> GetActivitiesForGroupAsync(int groupId)
    {
        return await _db.Activities
            .Where(a => a.GroupId == groupId && !a.IsArchived)
            .OrderBy(a => a.Name)
            .Select(a => new DrawActivityVm
            {
                Id = a.Id,
                Name = a.Name
            })
            .ToListAsync();
    }

    public async Task<List<DrawPresentationVm>> GetPresentationsForGroupAsync(int groupId)
    {
        return await _db.TaskItems
            .Where(t => t.IsPresentation && t.Activity.GroupId == groupId && !t.Activity.IsArchived)
            .OrderBy(t => t.Activity.Name).ThenBy(t => t.Title)
            .Select(t => new DrawPresentationVm
            {
                Id = t.Id,
                Title = t.Title,
                ActivityId = t.ActivityId,
                ActivityName = t.Activity.Name
            })
            .ToListAsync();
    }

    public async Task<List<string>> GetActiveStudentNamesForGroupAsync(int groupId)
    {
        return await _db.Students
            .Where(s => s.GroupId == groupId && s.IsActive)
            .Select(s => s.FirstName + " " + s.LastName)
            .ToListAsync();
    }
}
