using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;
using StudentApp.Web.Models.ViewModels;

namespace StudentApp.Web.Services;

public class EvaluationService : IEvaluationService
{
    private readonly AppDbContext _db;

    public EvaluationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<EvaluationIndexVm?> GetEvaluationIndexAsync(int groupId)
    {
        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) return null;

        var students = await _db.Students
            .Where(s => s.GroupId == groupId && s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync();

        var tasks = await _db.TaskItems
            .Where(t => t.Activity.GroupId == groupId && !t.Activity.IsArchived && !t.IsNumberedTask)
            .Include(t => t.Activity)
            .OrderBy(t => t.Activity.CreatedAt)
            .ToListAsync();

        var evaluations = await _db.Evaluations
            .Where(e => e.Student.GroupId == groupId)
            .ToListAsync();

        var scores = evaluations.ToDictionary(e => (e.StudentId, e.TaskItemId), e => e.Score);
        var evalIds = evaluations.ToDictionary(e => (e.StudentId, e.TaskItemId), e => e.Id);

        var studentAverages = new Dictionary<int, decimal>();
        foreach (var s in students)
        {
            var studentScores = evaluations.Where(e => e.StudentId == s.Id).Select(e => e.Score).ToList();
            if (studentScores.Count > 0)
                studentAverages[s.Id] = Math.Round(studentScores.Average(), 1);
        }

        var taskAverages = new Dictionary<int, decimal>();
        var taskSums = new Dictionary<int, decimal>();
        foreach (var t in tasks)
        {
            var taskScores = evaluations.Where(e => e.TaskItemId == t.Id).Select(e => e.Score).ToList();
            if (taskScores.Count > 0)
            {
                taskAverages[t.Id] = Math.Round(taskScores.Average(), 1);
                taskSums[t.Id] = taskScores.Sum();
            }
        }

        var activityStudentSums = new Dictionary<(int, int), decimal>();
        var tasksByActivity = tasks.GroupBy(t => t.ActivityId);
        foreach (var activityGroup in tasksByActivity)
        {
            var activityTaskIds = activityGroup.Select(t => t.Id).ToHashSet();
            foreach (var s in students)
            {
                var sum = evaluations
                    .Where(e => e.StudentId == s.Id && activityTaskIds.Contains(e.TaskItemId))
                    .Sum(e => e.Score);
                if (sum > 0)
                    activityStudentSums[(s.Id, activityGroup.Key)] = sum;
            }
        }

        var activityIds = tasks.Select(t => t.ActivityId).ToHashSet();
        var assignments = await _db.Assignments
            .Where(a => activityIds.Contains(a.ActivityId))
            .ToListAsync();

        // Count numbered task assignments per (student, activity) via PresentationStudents
        var numberedTaskIds = await _db.TaskItems
            .Where(t => activityIds.Contains(t.ActivityId) && t.IsNumberedTask)
            .Select(t => new { t.Id, t.ActivityId, t.Title })
            .ToListAsync();

        var numberedTaskActivityMap = numberedTaskIds.ToDictionary(t => t.Id, t => (t.ActivityId, t.Title));
        var numberedTaskIdSet = numberedTaskActivityMap.Keys.ToHashSet();

        var numberedPresStudents = await _db.PresentationStudents
            .Where(ps => numberedTaskIdSet.Contains(ps.TaskItemId) && ps.Role == PresentationRole.Presentee)
            .Select(ps => new { ps.StudentId, ps.TaskItemId })
            .ToListAsync();

        var numberedTaskCounts = numberedPresStudents
            .GroupBy(ps => (ps.StudentId, numberedTaskActivityMap[ps.TaskItemId].ActivityId))
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.Select(ps => numberedTaskActivityMap[ps.TaskItemId].Title).OrderBy(t => t)));

        var assignedStudentTasks = new HashSet<(int StudentId, int TaskItemId)>();

        // Any assignment to an activity (regardless of TaskItemId) means the student
        // can be evaluated for all regular (non-presentation) tasks in that activity
        var studentActivityPairs = assignments
            .Select(a => (a.StudentId, a.ActivityId))
            .Distinct()
            .ToHashSet();

        var nonPresentationTasks = tasks.Where(t => !t.IsPresentation).ToList();
        foreach (var t in nonPresentationTasks)
        {
            foreach (var (studentId, activityId) in studentActivityPairs)
            {
                if (activityId == t.ActivityId)
                    assignedStudentTasks.Add((studentId, t.Id));
            }
        }

        // Presentations are assigned via PresentationStudents, not Assignments — include them too
        var presentationTaskIds = tasks.Where(t => t.IsPresentation).Select(t => t.Id).ToHashSet();
        if (presentationTaskIds.Count > 0)
        {
            var presAssignments = await _db.PresentationStudents
                .Where(ps => presentationTaskIds.Contains(ps.TaskItemId))
                .Select(ps => new { ps.StudentId, ps.TaskItemId })
                .ToListAsync();

            foreach (var ps in presAssignments)
                assignedStudentTasks.Add((ps.StudentId, ps.TaskItemId));
        }

        return new EvaluationIndexVm
        {
            GroupId = groupId,
            GroupName = group.Name,
            Students = students.Select(s => new StudentSummaryVm
            {
                Id = s.Id,
                FullName = s.FullName,
                GroupId = s.GroupId
            }).ToList(),
            Tasks = tasks.Select(t => new TaskSummaryForEvalVm
            {
                Id = t.Id,
                Title = t.Title,
                ActivityId = t.ActivityId,
                ActivityName = t.Activity.Name,
                IsPresentation = t.IsPresentation,
                IsNumberedTask = t.IsNumberedTask,
                MaxScore = t.MaxScore
            }).ToList(),
            Scores = scores,
            EvaluationIds = evalIds,
            StudentAverages = studentAverages,
            TaskAverages = taskAverages,
            TaskSums = taskSums,
            ActivityStudentSums = activityStudentSums,
            AssignedStudentTasks = assignedStudentTasks,
            NumberedTaskCounts = numberedTaskCounts
        };
    }

    public async Task<int?> GetExistingEvaluationIdAsync(int studentId, int taskItemId)
    {
        var existing = await _db.Evaluations
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.TaskItemId == taskItemId);
        return existing?.Id;
    }

    public async Task<(string StudentName, string TaskName, decimal? MaxScore)?> GetStudentAndTaskInfoAsync(int studentId, int taskItemId)
    {
        var student = await _db.Students.FindAsync(studentId);
        var task = await _db.TaskItems.Include(t => t.Activity).FirstOrDefaultAsync(t => t.Id == taskItemId);
        if (student == null || task == null) return null;

        return (student.FullName, $"{task.Activity.Name} — {task.Title}", task.MaxScore);
    }

    public async Task<Evaluation> CreateEvaluationAsync(int studentId, int taskItemId, decimal score, string? comment)
    {
        var evaluation = new Evaluation
        {
            StudentId = studentId,
            TaskItemId = taskItemId,
            Score = Math.Round(score, 2),
            Comment = comment?.Trim()
        };
        _db.Evaluations.Add(evaluation);
        await _db.SaveChangesAsync();
        return evaluation;
    }

    public async Task<EvaluationEditVm?> GetEvaluationForEditAsync(int id)
    {
        var evaluation = await _db.Evaluations
            .Include(e => e.Student)
            .Include(e => e.TaskItem).ThenInclude(t => t.Activity)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evaluation == null) return null;

        return new EvaluationEditVm
        {
            Id = evaluation.Id,
            StudentId = evaluation.StudentId,
            TaskItemId = evaluation.TaskItemId,
            StudentName = evaluation.Student.FullName,
            TaskName = $"{evaluation.TaskItem.Activity.Name} — {evaluation.TaskItem.Title}",
            MaxScore = evaluation.TaskItem.MaxScore,
            Score = evaluation.Score,
            Comment = evaluation.Comment
        };
    }

    public async Task<bool> UpdateEvaluationAsync(int id, decimal score, string? comment)
    {
        var evaluation = await _db.Evaluations.FindAsync(id);
        if (evaluation == null) return false;

        evaluation.Score = Math.Round(score, 2);
        evaluation.Comment = comment?.Trim();
        evaluation.EvaluatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int?> GetStudentGroupIdAsync(int studentId)
    {
        var student = await _db.Students.FindAsync(studentId);
        return student?.GroupId;
    }
}
