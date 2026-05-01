namespace Tutorx.Web.Models.ViewModels;

public class CustomExportRequestVm
{
    public int GroupId { get; set; }
    public bool IncludeStudents { get; set; } = true;
    public bool IncludeStudentFirstName { get; set; } = true;
    public bool IncludeStudentLastName { get; set; } = true;
    public bool IncludeStudentCardNumber { get; set; } = true;
    public bool IncludeStudentYear { get; set; } = true;
    public bool IncludeStudentEmail { get; set; } = true;
    public bool IncludeStudentGroupNumber { get; set; } = true;
    public bool IncludeAttendance { get; set; }
    public bool IncludeAttendanceDetails { get; set; } = true;
    public bool IncludeAttendanceSummary { get; set; } = true;
    public bool IncludeActivities { get; set; }
    public bool IncludeTasks { get; set; }
    public bool IncludeTasksDetails { get; set; } = true;
    public bool IncludeTasksSummary { get; set; } = true;
    public bool IncludePresentations { get; set; }
    public bool IncludeOtherAttributes { get; set; }
    public string Format { get; set; } = "xlsx";
}

public class CustomExportIndexVm
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public CustomExportRequestVm Request { get; set; } = new();
}
