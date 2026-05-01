namespace StudentApp.Web.Models.Entities;

public class DrawHistory
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int GroupId { get; set; }
    public int CycleNumber { get; set; }
    public int DrawBatchId { get; set; }
    public DateTime DrawnAt { get; set; } = DateTime.UtcNow;

    public int? ActivityId { get; set; }
    public int? TaskItemId { get; set; }
    public PresentationRole? Role { get; set; }

    public Student Student { get; set; } = null!;
    public Group Group { get; set; } = null!;
    public Activity? Activity { get; set; }
    public TaskItem? TaskItem { get; set; }
}
