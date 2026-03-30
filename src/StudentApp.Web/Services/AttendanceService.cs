using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;

namespace StudentApp.Web.Services;

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _db;

    public AttendanceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Attendance>> GetOrCreateForDateAsync(int groupId, DateOnly date)
    {
        var existing = await _db.Attendances
            .Where(a => a.GroupId == groupId && a.Date == date)
            .Include(a => a.Student)
            .ToListAsync();

        if (existing.Count > 0)
            return existing;

        var activeStudents = await _db.Students
            .Where(s => s.GroupId == groupId && s.IsActive)
            .ToListAsync();

        var records = activeStudents.Select(s => new Attendance
        {
            StudentId = s.Id,
            GroupId = groupId,
            Date = date,
            Status = AttendanceStatus.Present
        }).ToList();

        return records;
    }

    public async Task SaveAttendanceAsync(int groupId, DateOnly date, List<(int StudentId, AttendanceStatus Status)> records)
    {
        foreach (var (studentId, status) in records)
        {
            var existing = await _db.Attendances
                .FirstOrDefaultAsync(a => a.StudentId == studentId && a.GroupId == groupId && a.Date == date);

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
}
