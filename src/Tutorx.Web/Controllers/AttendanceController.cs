using Microsoft.AspNetCore.Mvc;
using Tutorx.Web.Models.Entities;
using Tutorx.Web.Models.ViewModels;
using Tutorx.Web.Services;

namespace Tutorx.Web.Controllers;

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
    public async Task<IActionResult> Record(int? groupId, DateOnly? date, TimeOnly? time)
    {
        await PopulateActiveGroupAsync();
        var gid = groupId ?? HttpContext.Session.GetActiveGroup();
        if (!gid.HasValue)
        {
            ViewBag.NoGroupSelected = true;
            return View((AttendanceRecordVm?)null);
        }

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var vm = await _attendanceService.GetAttendanceRecordAsync(gid.Value, targetDate, time);
        if (vm == null) return NotFound();

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Record(AttendanceRecordVm vm)
    {
        var records = vm.Rows
            .Where(r => r.Status.HasValue)
            .Select(r => (r.StudentId, r.Status!.Value)).ToList();
        await _attendanceService.SaveAttendanceAsync(vm.GroupId, vm.Date, vm.Time, records);

        TempData["Success"] = $"Dochádzka uložená pre {vm.Date:dd.MM.yyyy}{(vm.Time.HasValue ? $" {vm.Time.Value:HH:mm}" : "")}.";
        var redirectParams = new { groupId = vm.GroupId, date = vm.Date.ToString("yyyy-MM-dd"), time = vm.Time.HasValue ? vm.Time.Value.ToString("HH:mm") : null };
        return RedirectToAction(nameof(Record), redirectParams);
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
