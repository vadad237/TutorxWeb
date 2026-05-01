namespace Tutorx.Web.Models.Entities;

public enum AttendanceStatus { Present, Absent, Excused }

public class Attendance
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int GroupId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    public Student Student { get; set; } = null!;
    public Group Group { get; set; } = null!;
}
