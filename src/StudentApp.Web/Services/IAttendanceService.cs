using StudentApp.Web.Models.Entities;

namespace StudentApp.Web.Services;

public interface IAttendanceService
{
    Task<List<Attendance>> GetOrCreateForDateAsync(int groupId, DateOnly date);
    Task SaveAttendanceAsync(int groupId, DateOnly date, List<(int StudentId, AttendanceStatus Status)> records);
    Task<List<Attendance>> GetHistoryAsync(int groupId, DateOnly? from = null, DateOnly? to = null);
}
