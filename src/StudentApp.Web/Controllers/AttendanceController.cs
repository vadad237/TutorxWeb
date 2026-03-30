using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;
using StudentApp.Web.Models.ViewModels;
using StudentApp.Web.Services;

namespace StudentApp.Web.Controllers;

public class AttendanceController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(AppDbContext db, IAttendanceService attendanceService)
    {
        _db = db;
        _attendanceService = attendanceService;
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
    public async Task<IActionResult> Record(int? groupId, DateOnly? date)
    {
        await PopulateActiveGroupAsync();
        var gid = groupId ?? HttpContext.Session.GetActiveGroup();
        if (!gid.HasValue)
        {
            ViewBag.NoGroupSelected = true;
            return View((AttendanceRecordVm?)null);
        }

        var group = await _db.Groups.FindAsync(gid.Value);
        if (group == null) return NotFound();

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var records = await _attendanceService.GetOrCreateForDateAsync(gid.Value, targetDate);

        // Load students for records that aren't saved yet
        var activeStudents = await _db.Students
            .Where(s => s.GroupId == gid.Value && s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync();

        var existingMap = records.Where(r => r.Id > 0).ToDictionary(r => r.StudentId, r => r.Status);

        var vm = new AttendanceRecordVm
        {
            GroupId = gid.Value,
            GroupName = group.Name,
            Date = targetDate,
            Rows = activeStudents.Select(s => new StudentAttendanceRowVm
            {
                StudentId = s.Id,
                FullName = s.FullName,
                Status = existingMap.TryGetValue(s.Id, out var status) ? status : null
            }).ToList()
        };

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

        var group = await _db.Groups.FindAsync(gid.Value);
        if (group == null) return NotFound();

        var students = await _db.Students
            .Where(s => s.GroupId == gid.Value && s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync();

        var attendances = await _db.Attendances
            .Where(a => a.GroupId == gid.Value)
            .ToListAsync();

        var dates = attendances
            .Select(a => a.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(30)
            .OrderBy(d => d)
            .ToList();

        var statusMap = attendances.ToDictionary(a => (a.StudentId, a.Date), a => a.Status);

        var vm = new AttendanceHistoryVm
        {
            GroupId = gid.Value,
            GroupName = group.Name,
            Students = students.Select(s => new StudentSummaryVm
            {
                Id = s.Id,
                FullName = s.FullName,
                GroupId = s.GroupId
            }).ToList(),
            Dates = dates,
            StatusMap = statusMap
        };

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

        var group = await _db.Groups.FindAsync(gid.Value);
        if (group == null) return NotFound();

        var students = await _db.Students
            .Where(s => s.GroupId == gid.Value && s.IsActive)
            .Include(s => s.Attendances)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync();

        var vm = new AttendanceSummaryVm
        {
            GroupId = gid.Value,
            GroupName = group.Name,
            Items = students.Select(s =>
            {
                var total = s.Attendances.Count;
                var present = s.Attendances.Count(a => a.Status == AttendanceStatus.Present);
                return new AttendanceSummaryItemVm
                {
                    StudentId = s.Id,
                    FullName = s.FullName,
                    PresentCount = present,
                    AbsentCount = s.Attendances.Count(a => a.Status == AttendanceStatus.Absent),
                    ExcusedCount = s.Attendances.Count(a => a.Status == AttendanceStatus.Excused),
                    TotalCount = total,
                    AttendancePercentage = total > 0 ? Math.Round((double)present / total * 100, 1) : 0
                };
            }).ToList()
        };

        return View(vm);
    }
}
