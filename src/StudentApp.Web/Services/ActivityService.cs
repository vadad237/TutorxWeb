using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;
using StudentApp.Web.Models.ViewModels;

namespace StudentApp.Web.Services;

public class ActivityService : IActivityService
{
    private readonly AppDbContext _db;

    public ActivityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ActivitySummaryVm>> GetActivitySummariesAsync(int groupId)
    {
        var rawActivities = await _db.Activities
            .Where(a => !a.IsArchived && a.GroupId == groupId)
            .Include(a => a.Group)
            .Include(a => a.Tasks)
            .Include(a => a.Assignments).ThenInclude(asgn => asgn.Student)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return rawActivities.Select(a => new ActivitySummaryVm
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
    }

    public async Task<(int Id, string Name)?> GetGroupInfoAsync(int groupId)
    {
        var group = await _db.Groups.Where(g => g.Id == groupId && !g.IsArchived)
            .Select(g => new { g.Id, g.Name }).FirstOrDefaultAsync();
        if (group == null) return null;
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
        if (activity == null) return null;

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

        if (activity == null) return null;

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
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new SimpleTaskVm(t.Id, t.Title, t.CreatedAt))
                .ToList(),
            NumberedTasks = activity.Tasks
                .Where(t => t.IsNumberedTask)
                .OrderBy(t => t.Title.Length).ThenBy(t => t.Title)
                .Select((t, i) => new NumberedTaskVm(
                    t.Id,
                    int.TryParse(t.Title, out var n) ? n : i + 1,
                    t.PresentationStudents
                        .Where(ps => ps.Role == PresentationRole.Presentee)
                        .Select(ps => new PresentationStudentVm(ps.StudentId, ps.Student.FirstName + " " + ps.Student.LastName))
                        .OrderBy(ps => ps.FullName)
                        .ToList()
                ))
                .ToList(),
            Presentations = activity.Tasks
                .Where(t => t.IsPresentation)
                .OrderBy(t => t.Title)
                .Select(t => new TaskWithAssignmentVm(
                    t.Id,
                    t.Title,
                    t.PresentationDate,
                    t.PresentationStudents
                        .Where(ps => ps.Role == PresentationRole.Presentee)
                        .Select(ps => new PresentationStudentVm(ps.StudentId, ps.Student.FirstName + " " + ps.Student.LastName))
                        .OrderBy(ps => ps.FullName)
                        .ToList(),
                    t.PresentationStudents
                        .Where(ps => ps.Role == PresentationRole.Substitution)
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
            .Select(s => new StudentSummaryVm { Id = s.Id, FullName = s.FirstName + " " + s.LastName })
            .ToList();

        return (details, allStudents);
    }

    public async Task<(bool Success, string Message)> ArchiveActivityAsync(int id)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity == null)
            return (false, "Activity not found.");

        activity.IsArchived = true;
        await _db.SaveChangesAsync();
        return (true, $"Activity '{activity.Name}' archived.");
    }

    public async Task<(bool Exists, bool Deleted)> DeleteActivityAsync(int id)
    {
        var exists = await _db.Activities.AnyAsync(a => a.Id == id);
        if (!exists) return (false, false);

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

        if (activity == null) return null;

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

    public async Task<(string ActivityName, List<string> AllStudentNames)?> GetDrawResultDataAsync(int activityId)
    {
        var activity = await _db.Activities
            .Include(a => a.Group).ThenInclude(g => g.Students)
            .FirstOrDefaultAsync(a => a.Id == activityId);

        if (activity == null) return null;

        var allStudentNames = activity.Group.Students
            .Where(s => s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => $"{s.FirstName} {s.LastName}")
            .ToList();

        return (activity.Name, allStudentNames);
    }

    public async Task<(int ActivityId, string ActivityName, string TaskTitle, List<string> AllStudentNames)?> GetPresentationDrawResultDataAsync(int taskId)
    {
        var task = await _db.TaskItems
            .Include(t => t.Activity)
                .ThenInclude(a => a.Group)
                    .ThenInclude(g => g.Students)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null) return null;

        var allStudentNames = task.Activity.Group.Students
            .Where(s => s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => $"{s.FirstName} {s.LastName}")
            .ToList();

        return (task.ActivityId, task.Activity.Name, task.Title, allStudentNames);
    }

    public async Task<(bool Success, int? NewId, string? Message)> DuplicateActivityAsync(int id)
    {
        var source = await _db.Activities
            .Include(a => a.Tasks)
            .Include(a => a.Assignments)
            .Include(a => a.OtherAttributes).ThenInclude(attr => attr.Options)
            .Include(a => a.OtherAttributes).ThenInclude(attr => attr.StudentValues)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (source == null)
            return (false, null, "Activity not found.");

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

        return (true, copy.Id, null);
    }

    public async Task<bool> SetActivityAssignmentsAsync(int activityId, int[]? studentIds)
    {
        var activityExists = await _db.Activities.AnyAsync(a => a.Id == activityId);
        if (!activityExists) return false;

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
