using Microsoft.AspNetCore.Mvc;
using StudentApp.Web.Models.DTOs;
using StudentApp.Web.Models.ViewModels;
using StudentApp.Web.Services;

namespace StudentApp.Web.Controllers;

public class StudentsController : Controller
{
    private readonly IGroupService _groupService;
    private readonly IStudentService _studentService;
    private readonly IImportService _importService;

    public StudentsController(IGroupService groupService, IStudentService studentService, IImportService importService)
    {
        _groupService = groupService;
        _studentService = studentService;
        _importService = importService;
    }

    private async Task PopulateActiveGroupAsync()
    {
        var groups = await _groupService.GetNonArchivedGroupsAsync();
        ViewBag.AllGroups = groups.Select(g => new { g.Id, g.Name }).ToList();
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

        var groupName = await _groupService.GetGroupNameAsync(gid.Value);
        if (groupName == null) return NotFound();

        ViewBag.GroupId = gid.Value;
        ViewBag.GroupName = groupName;

        var students = await _studentService.GetStudentsByGroupAsync(gid.Value);
        return View(students);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int groupId)
    {
        await PopulateActiveGroupAsync();
        var groupName = await _groupService.GetGroupNameAsync(groupId);
        if (groupName == null) return NotFound();
        ViewBag.GroupName = groupName;

        return View(new StudentCreateVm { GroupId = groupId });
    }

    [HttpPost]
    public async Task<IActionResult> Create(StudentCreateVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            var groupName = await _groupService.GetGroupNameAsync(vm.GroupId);
            ViewBag.GroupName = groupName;
            return View(vm);
        }

        var student = await _studentService.CreateStudentAsync(vm.FirstName, vm.LastName, vm.Email, vm.CardNumber, vm.Year, vm.GroupId);

        TempData["Success"] = $"Študent '{student.FullName}' bol úspešne pridaný.";
        return RedirectToAction(nameof(Index), new { groupId = vm.GroupId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        await PopulateActiveGroupAsync();
        var student = await _studentService.GetStudentWithGroupAsync(id);
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
    public async Task<IActionResult> Edit(int id, StudentEditVm vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        var student = await _studentService.UpdateStudentAsync(id, vm.FirstName, vm.LastName, vm.Email, vm.CardNumber, vm.Year, vm.IsActive);
        if (student == null) return NotFound();

        TempData["Success"] = $"Študent '{student.FullName}' bol úspešne aktualizovaný.";
        return RedirectToAction(nameof(Index), new { groupId = vm.GroupId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        await PopulateActiveGroupAsync();
        var vm = await _studentService.GetStudentDetailsAsync(id);
        if (vm == null) return NotFound();

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _studentService.DeleteStudentAsync(id);
        if (!deleted)
            return Json(new { success = false, message = "Student not found." });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> BulkDelete([FromBody] int[]? studentIds)
    {
        if (studentIds == null || studentIds.Length == 0)
            return Json(new { success = false, message = "No students selected." });

        await _studentService.BulkDeleteStudentsAsync(studentIds.ToList());
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var student = await _studentService.ToggleActiveAsync(id);
        if (student == null) return NotFound();

        TempData["Success"] = $"Študent '{student.FullName}' je teraz {(student.IsActive ? "aktívny" : "neaktívny")}.";
        return RedirectToAction(nameof(Index), new { groupId = student.GroupId });
    }

    [HttpPost]
    public async Task<IActionResult> BulkSetActive([FromBody] BulkSetActiveRequest? request)
    {
        if (request?.StudentIds == null || request.StudentIds.Length == 0)
            return Json(new { success = false, message = "No students selected." });

        await _studentService.BulkSetActiveAsync(request.StudentIds.ToList(), request.Active);
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> Import(int groupId)
    {
        await PopulateActiveGroupAsync();
        var groupName = await _groupService.GetGroupNameAsync(groupId);
        if (groupName == null) return NotFound();

        return View(new ImportUploadVm { GroupId = groupId, GroupName = groupName });
    }

    [HttpPost]
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
