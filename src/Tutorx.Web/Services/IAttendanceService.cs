using Tutorx.Web.Models.Entities;
using Tutorx.Web.Models.ViewModels;

namespace Tutorx.Web.Services;

public interface IAttendanceService
{
    Task<List<Attendance>> GetOrCreateForDateAsync(int groupId, DateOnly date, TimeOnly? time);
    Task SaveAttendanceAsync(int groupId, DateOnly date, TimeOnly? time, List<(int StudentId, AttendanceStatus Status)> records);
    Task<List<Attendance>> GetHistoryAsync(int groupId, DateOnly? from = null, DateOnly? to = null);
    Task<AttendanceRecordVm?> GetAttendanceRecordAsync(int groupId, DateOnly date, TimeOnly? time);
    Task<AttendanceHistoryVm?> GetAttendanceHistoryAsync(int groupId);
    Task<AttendanceSummaryVm?> GetAttendanceSummaryAsync(int groupId);
}
