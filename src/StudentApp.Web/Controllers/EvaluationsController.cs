using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;
using StudentApp.Web.Models.ViewModels;

namespace StudentApp.Web.Controllers;

public class EvaluationsController : Controller
{
    private readonly AppDbContext _db;

    public EvaluationsController(AppDbContext db)
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

    [HttpGet]
    public async Task<IActionResult> Index(int? groupId)
    {
        await PopulateActiveGroupAsync();
        var gid = groupId ?? HttpContext.Session.GetActiveGroup();
        if (!gid.HasValue)
        {
            ViewBag.NoGroupSelected = true;
            return View((EvaluationIndexVm?)null);
        }

        var group = await _db.Groups.FindAsync(gid.Value);
        if (group == null) return NotFound();

        var students = await _db.Students
            .Where(s => s.GroupId == gid.Value && s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync();

        var tasks = await _db.TaskItems
            .Where(t => t.Activity.GroupId == gid.Value && !t.Activity.IsArchived)
            .Include(t => t.Activity)
            .OrderBy(t => t.Activity.Name).ThenBy(t => t.Title)
            .ToListAsync();

        var evaluations = await _db.Evaluations
            .Where(e => e.Student.GroupId == gid.Value)
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
        foreach (var group2 in tasksByActivity)
        {
            var activityTaskIds = group2.Select(t => t.Id).ToHashSet();
            foreach (var s in students)
            {
                var sum = evaluations
                    .Where(e => e.StudentId == s.Id && activityTaskIds.Contains(e.TaskItemId))
                    .Sum(e => e.Score);
                if (sum > 0)
                    activityStudentSums[(s.Id, group2.Key)] = sum;
            }
        }

        var vm = new EvaluationIndexVm
        {
            GroupId = gid.Value,
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
                IsPresentation = t.IsPresentation
            }).ToList(),
            Scores = scores,
            EvaluationIds = evalIds,
            StudentAverages = studentAverages,
            TaskAverages = taskAverages,
            TaskSums = taskSums,
            ActivityStudentSums = activityStudentSums
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int studentId, int taskItemId)
    {
        await PopulateActiveGroupAsync();
        var student = await _db.Students.FindAsync(studentId);
        var task = await _db.TaskItems.Include(t => t.Activity).FirstOrDefaultAsync(t => t.Id == taskItemId);
        if (student == null || task == null) return NotFound();

        var existing = await _db.Evaluations
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.TaskItemId == taskItemId);
        if (existing != null)
            return RedirectToAction(nameof(Edit), new { id = existing.Id });

        var vm = new EvaluationCreateVm
        {
            StudentId = studentId,
            TaskItemId = taskItemId,
            StudentName = student.FullName,
            TaskName = $"{task.Activity.Name} — {task.Title}"
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EvaluationCreateVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        var evaluation = new Evaluation
        {
            StudentId = vm.StudentId,
            TaskItemId = vm.TaskItemId,
            Score = vm.Score,
            Comment = vm.Comment?.Trim()
        };
        _db.Evaluations.Add(evaluation);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Hodnotenie uložené.";
        var student = await _db.Students.FindAsync(vm.StudentId);
        return RedirectToAction(nameof(Index), new { groupId = student?.GroupId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        await PopulateActiveGroupAsync();
        var evaluation = await _db.Evaluations
            .Include(e => e.Student)
            .Include(e => e.TaskItem).ThenInclude(t => t.Activity)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evaluation == null) return NotFound();

        var vm = new EvaluationEditVm
        {
            Id = evaluation.Id,
            StudentId = evaluation.StudentId,
            TaskItemId = evaluation.TaskItemId,
            StudentName = evaluation.Student.FullName,
            TaskName = $"{evaluation.TaskItem.Activity.Name} — {evaluation.TaskItem.Title}",
            Score = evaluation.Score,
            Comment = evaluation.Comment
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EvaluationEditVm vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        var evaluation = await _db.Evaluations.FindAsync(id);
        if (evaluation == null) return NotFound();

        evaluation.Score = vm.Score;
        evaluation.Comment = vm.Comment?.Trim();
        evaluation.EvaluatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Hodnotenie aktualizované.";
        var student = await _db.Students.FindAsync(vm.StudentId);
        return RedirectToAction(nameof(Index), new { groupId = student?.GroupId });
    }
}
