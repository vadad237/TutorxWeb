using Microsoft.AspNetCore.Mvc;
using StudentApp.Web.Models.Entities;
using StudentApp.Web.Models.ViewModels;
using StudentApp.Web.Services;

namespace StudentApp.Web.Controllers;

public class AttendanceController : Controller
{
    private readonly IGroupService _groupService;
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IGroupService groupService, IAttendanceService attendanceService)
    {
        _groupService = groupService;
        _attendanceService = attendanceService;
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

    [HttpGet]
    public async Task<IActionResult> Record(int? groupId, DateOnly? date)
    {
        await PopulateActiveGroupAsync();
        var gid = groupId ?? HttpContext.Session.GetActiveGroup();
        if (!gid.HasValue)
        {
            ViewBag.NoGroupSelected = true;
            return View((AttendanceRecordVm?)null);
        }

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var vm = await _attendanceService.GetAttendanceRecordAsync(gid.Value, targetDate);
        if (vm == null) return NotFound();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(AttendanceRecordVm vm)
    {
        var records = vm.Rows
            .Where(r => r.Status.HasValue)
            .Select(r => (r.StudentId, r.Status!.Value)).ToList();
        await _attendanceService.SaveAttendanceAsync(vm.GroupId, vm.Date, records);

        TempData["Success"] = $"Dochádzka uložená pre {vm.Date:dd.MM.yyyy}.";
        return RedirectToAction(nameof(Record), new { groupId = vm.GroupId, date = vm.Date.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSingle(int groupId, DateOnly date, int studentId, AttendanceStatus status)
    {
        await _attendanceService.SaveAttendanceAsync(groupId, date, [(studentId, status)]);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> History(int? groupId)
    {
        await PopulateActiveGroupAsync();
        var gid = groupId ?? HttpContext.Session.GetActiveGroup();
        if (!gid.HasValue)
        {
            TempData["Error"] = "Najskôr vyberte skupinu.";
            return RedirectToAction("Index", "Groups");
        }

        var vm = await _attendanceService.GetAttendanceHistoryAsync(gid.Value);
        if (vm == null) return NotFound();

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Summary(int? groupId)
    {
        await PopulateActiveGroupAsync();
        var gid = groupId ?? HttpContext.Session.GetActiveGroup();
        if (!gid.HasValue)
        {
            TempData["Error"] = "Najskôr vyberte skupinu.";
            return RedirectToAction("Index", "Groups");
        }

        var vm = await _attendanceService.GetAttendanceSummaryAsync(gid.Value);
        if (vm == null) return NotFound();

        return View(vm);
    }
}
