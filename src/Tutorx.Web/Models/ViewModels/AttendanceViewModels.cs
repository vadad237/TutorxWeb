using Tutorx.Web.Models.Entities;

namespace Tutorx.Web.Models.ViewModels;

public class AttendanceRecordVm
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public List<StudentAttendanceRowVm> Rows { get; set; } = [];
}

public class StudentAttendanceRowVm
{
    public int StudentId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? GroupNumber { get; set; }
    public AttendanceStatus? Status { get; set; }
}

public class AttendanceHistoryVm
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public List<StudentSummaryVm> Students { get; set; } = [];
    public List<(DateOnly Date, TimeOnly? Time)> Dates { get; set; } = [];
    public Dictionary<(int StudentId, DateOnly Date, TimeOnly? Time), AttendanceStatus> StatusMap { get; set; } = [];
}

public class AttendanceSummaryVm
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public List<AttendanceSummaryItemVm> Items { get; set; } = [];
}

public class AttendanceSummaryItemVm
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = null!;
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
    public int TotalCount { get; set; }
    public double AttendancePercentage { get; set; }
}
