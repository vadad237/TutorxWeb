using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.DTOs;
using StudentApp.Web.Models.Entities;
using StudentApp.Web.Models.ViewModels;
using StudentApp.Web.Services;

namespace StudentApp.Web.Controllers;

public class StudentsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IImportService _importService;

    public StudentsController(AppDbContext db, IImportService importService)
    {
        _db = db;
        _importService = importService;
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

    public async Task<IActionResult> Index(int? groupId)
    {
        await PopulateActiveGroupAsync();
        var gid = groupId ?? HttpContext.Session.GetActiveGroup();
        if (!gid.HasValue)
        {
            ViewBag.NoGroupSelected = true;
            return View(new List<StudentSummaryVm>());
        }

        var group = await _db.Groups.FindAsync(gid.Value);
        if (group == null) return NotFound();

        ViewBag.GroupId = gid.Value;
        ViewBag.GroupName = group.Name;

        var students = await _db.Students
            .Where(s => s.GroupId == gid.Value)
            .Include(s => s.Attendances)
            .Include(s => s.Evaluations)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new StudentSummaryVm
            {
                Id = s.Id,
                FullName = s.FirstName + " " + s.LastName,
                Email = s.Email,
                CardNumber = s.CardNumber,
                Year = s.Year,
                IsActive = s.IsActive,
                AbsenceCount = s.Attendances.Count(a => a.Status == AttendanceStatus.Absent),
                AvgScore = s.Evaluations.Any() ? s.Evaluations.Average(e => e.Score) : null,
                GroupId = s.GroupId
            })
            .ToListAsync();

        return View(students);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int groupId)
    {
        await PopulateActiveGroupAsync();
        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) return NotFound();
        ViewBag.GroupName = group.Name;

        return View(new StudentCreateVm { GroupId = groupId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentCreateVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            var group = await _db.Groups.FindAsync(vm.GroupId);
            ViewBag.GroupName = group?.Name;
            return View(vm);
        }

        var student = new Student
        {
            FirstName = vm.FirstName.Trim(),
            LastName = vm.LastName.Trim(),
            Email = vm.Email?.Trim(),
            CardNumber = vm.CardNumber?.Trim(),
            Year = vm.Year,
            GroupId = vm.GroupId
        };
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Študent '{student.FullName}' bol úspešne pridaný.";
        return RedirectToAction(nameof(Index), new { groupId = vm.GroupId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        await PopulateActiveGroupAsync();
        var student = await _db.Students.Include(s => s.Group).FirstOrDefaultAsync(s => s.Id == id);
        if (student == null) return NotFound();
        ViewBag.GroupName = student.Group.Name;

        return View(new StudentEditVm
        {
            Id = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Email = student.Email,
            CardNumber = student.CardNumber,
            Year = student.Year,
            IsActive = student.IsActive,
            GroupId = student.GroupId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudentEditVm vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        var student = await _db.Students.FindAsync(id);
        if (student == null) return NotFound();

        student.FirstName = vm.FirstName.Trim();
        student.LastName = vm.LastName.Trim();
        student.Email = vm.Email?.Trim();
        student.CardNumber = vm.CardNumber?.Trim();
        student.Year = vm.Year;
        student.IsActive = vm.IsActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Študent '{student.FullName}' bol úspešne aktualizovaný.";
        return RedirectToAction(nameof(Index), new { groupId = vm.GroupId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        await PopulateActiveGroupAsync();
        var student = await _db.Students
            .Include(s => s.Group)
            .Include(s => s.DrawHistories)
            .Include(s => s.Attendances)
            .Include(s => s.Evaluations).ThenInclude(e => e.TaskItem).ThenInclude(t => t.Activity)
            .Include(s => s.Assignments).ThenInclude(a => a.Activity)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null) return NotFound();

        var vm = new StudentDetailsVm
        {
            Id = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            FullName = student.FullName,
            Email = student.Email,
            CardNumber = student.CardNumber,
            Year = student.Year,
            IsActive = student.IsActive,
            GroupId = student.GroupId,
            GroupName = student.Group.Name,
            CreatedAt = student.CreatedAt,
            DrawHistory = student.DrawHistories
                .OrderByDescending(d => d.DrawnAt)
                .Select(d => new DrawHistoryDto(student.FullName, d.DrawnAt, d.CycleNumber))
                .ToList(),
            AttendanceSummary = new AttendanceSummaryItemVm
            {
                StudentId = student.Id,
                FullName = student.FullName,
                PresentCount = student.Attendances.Count(a => a.Status == AttendanceStatus.Present),
                AbsentCount = student.Attendances.Count(a => a.Status == AttendanceStatus.Absent),
                ExcusedCount = student.Attendances.Count(a => a.Status == AttendanceStatus.Excused),
                TotalCount = student.Attendances.Count
            },
            Evaluations = student.Evaluations
                .OrderBy(e => e.TaskItem.Activity.Name).ThenBy(e => e.TaskItem.Title)
                .Select(e => new EvaluationItemVm
                {
                    Id = e.Id,
                    TaskName = e.TaskItem.Activity.Name + " — " + e.TaskItem.Title,
                    Score = e.Score,
                    Comment = e.Comment,
                    EvaluatedAt = e.EvaluatedAt
                }).ToList(),
            AssignedActivities = student.Assignments
                .Where(a => !a.Activity.IsArchived)
                .OrderBy(a => a.Activity.Name)
                .Select(a => new AssignedActivityVm(a.ActivityId, a.Activity.Name))
                .ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await _db.Students.FindAsync(id);
        if (student == null)
            return Json(new { success = false, message = "Student not found." });

        // Clear all Restrict FK children before removing the student
        await _db.StudentAttributeValues.Where(v => v.StudentId == id).ExecuteDeleteAsync();
        await _db.PresentationStudents.Where(ps => ps.StudentId == id).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => e.StudentId == id).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => a.StudentId == id).ExecuteDeleteAsync();
        await _db.DrawHistories.Where(d => d.StudentId == id).ExecuteDeleteAsync();
        await _db.Attendances.Where(a => a.StudentId == id).ExecuteDeleteAsync();

        _db.Students.Remove(student);
        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> BulkDelete([FromBody] int[]? studentIds)
    {
        if (studentIds == null || studentIds.Length == 0)
            return Json(new { success = false, message = "No students selected." });

        var ids = studentIds.ToList();

        await _db.StudentAttributeValues.Where(v => ids.Contains(v.StudentId)).ExecuteDeleteAsync();
        await _db.PresentationStudents.Where(ps => ids.Contains(ps.StudentId)).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => ids.Contains(e.StudentId)).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => ids.Contains(a.StudentId)).ExecuteDeleteAsync();
        await _db.DrawHistories.Where(d => ids.Contains(d.StudentId)).ExecuteDeleteAsync();
        await _db.Attendances.Where(a => ids.Contains(a.StudentId)).ExecuteDeleteAsync();

        await _db.Students.Where(s => ids.Contains(s.Id)).ExecuteDeleteAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var student = await _db.Students.FindAsync(id);
        if (student == null) return NotFound();

        student.IsActive = !student.IsActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Študent '{student.FullName}' je teraz {(student.IsActive ? "aktívny" : "neaktívny")}.";
        return RedirectToAction(nameof(Index), new { groupId = student.GroupId });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> BulkSetActive([FromBody] BulkSetActiveRequest? request)
    {
        if (request?.StudentIds == null || request.StudentIds.Length == 0)
            return Json(new { success = false, message = "No students selected." });

        var ids = request.StudentIds.ToList();
        await _db.Students.Where(s => ids.Contains(s.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, request.Active));

        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> Import(int groupId)
    {
        await PopulateActiveGroupAsync();
        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) return NotFound();

        return View(new ImportUploadVm { GroupId = groupId, GroupName = group.Name });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportPreview(IFormFile file, int groupId)
    {
        await PopulateActiveGroupAsync();
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Prosím vyberte súbor na import.";
            return RedirectToAction(nameof(Import), new { groupId });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var preview = await _importService.ParseFileAsync(stream, file.FileName, groupId);

            var vm = new ImportPreviewVm
            {
                GroupId = preview.GroupId,
                GroupName = preview.GroupName,
                Rows = preview.Rows.Select(r => new ImportRowVm
                {
                    FirstName = r.FirstName,
                    LastName = r.LastName,
                    Email = r.Email,
                    CardNumber = r.CardNumber,
                    Year = r.Year,
                    Status = r.Status.ToString(),
                    ErrorMessage = r.ErrorMessage,
                    Selected = r.Status != ImportRowStatus.Error
                }).ToList()
            };

            return View("ImportPreview", vm);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Chyba importu: {ex.Message}";
            return RedirectToAction(nameof(Import), new { groupId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportConfirm(ImportConfirmVm vm)
    {
        var rowsToImport = vm.Rows
            .Where(r => r.Selected && r.Status != "Error")
            .Select(r => new ImportRowDto(r.FirstName, r.LastName, r.Email, r.CardNumber, r.Year, ImportRowStatus.Valid, null))
            .ToList();

        var count = await _importService.ImportStudentsAsync(vm.GroupId, rowsToImport);

        TempData["Success"] = $"Úspešne importovaných {count} študentov.";
        return RedirectToAction(nameof(Index), new { groupId = vm.GroupId });
    }
}
