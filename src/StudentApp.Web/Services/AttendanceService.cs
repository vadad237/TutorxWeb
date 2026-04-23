using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;
using StudentApp.Web.Models.ViewModels;

namespace StudentApp.Web.Services;

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _db;

    public AttendanceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Attendance>> GetOrCreateForDateAsync(int groupId, DateOnly date, TimeOnly? time)
    {
        var existing = await _db.Attendances
            .Where(a => a.GroupId == groupId && a.Date == date && a.Time == time)
            .Include(a => a.Student)
            .ToListAsync();

        if (existing.Count > 0)
            return existing;

        var activeStudents = await _db.Students
            .Where(s => s.GroupId == groupId && s.IsActive)
            .ToListAsync();

        return activeStudents.Select(s => new Attendance
        {
            StudentId = s.Id,
            GroupId = groupId,
            Date = date,
            Time = time,
            Status = AttendanceStatus.Present
        }).ToList();
    }

    public async Task SaveAttendanceAsync(int groupId, DateOnly date, TimeOnly? time, List<(int StudentId, AttendanceStatus Status)> records)
    {
        foreach (var (studentId, status) in records)
        {
            var existing = await _db.Attendances
                .FirstOrDefaultAsync(a => a.StudentId == studentId && a.GroupId == groupId && a.Date == date && a.Time == time);

            if (existing != null)
            {
                existing.Status = status;
            }
            else
            {
                _db.Attendances.Add(new Attendance
                {
                    StudentId = studentId,
                    GroupId = groupId,
                    Date = date,
                    Time = time,
                    Status = status
                });
            }
        }
        await _db.SaveChangesAsync();
    }


    public async Task<List<Attendance>> GetHistoryAsync(int groupId, DateOnly? from = null, DateOnly? to = null)
    {
        var query = _db.Attendances
            .Where(a => a.GroupId == groupId);

        if (from.HasValue)
            query = query.Where(a => a.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.Date <= to.Value);

        return await query
            .Include(a => a.Student)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }

    public async Task<AttendanceRecordVm?> GetAttendanceRecordAsync(int groupId, DateOnly date, TimeOnly? time)
    {
        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) return null;

        var records = await GetOrCreateForDateAsync(groupId, date, time);

        var activeStudents = await _db.Students
            .Where(s => s.GroupId == groupId && s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync();

        var existingMap = records.Where(r => r.Id > 0).ToDictionary(r => r.StudentId, r => r.Status);

        return new AttendanceRecordVm
        {
            GroupId = groupId,
            GroupName = group.Name,
            Date = date,
            Time = time,
            Rows = activeStudents.Select(s => new StudentAttendanceRowVm
            {
                StudentId = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                FullName = s.FullName,
                GroupNumber = s.GroupNumber,
                Status = existingMap.TryGetValue(s.Id, out var status) ? status : null
            }).ToList()
        };
    }

    public async Task<AttendanceHistoryVm?> GetAttendanceHistoryAsync(int groupId)
    {
        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) return null;

        var students = await _db.Students
            .Where(s => s.GroupId == groupId && s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync();

        var attendances = await _db.Attendances
            .Where(a => a.GroupId == groupId)
            .ToListAsync();

        var dates = attendances
            .Select(a => (a.Date, a.Time))
            .Distinct()
            .OrderByDescending(d => d)
            .Take(30)
            .OrderBy(d => d)
            .ToList();

        var statusMap = attendances
            .GroupBy(a => (a.StudentId, a.Date, a.Time))
            .ToDictionary(g => g.Key, g => g.Last().Status);

        return new AttendanceHistoryVm
        {
            GroupId = groupId,
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
    }

    public async Task<AttendanceSummaryVm?> GetAttendanceSummaryAsync(int groupId)
    {
        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) return null;

        var students = await _db.Students
            .Where(s => s.GroupId == groupId && s.IsActive)
            .Include(s => s.Attendances)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync();

        return new AttendanceSummaryVm
        {
            GroupId = groupId,
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
    }
}
