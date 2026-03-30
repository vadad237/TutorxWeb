namespace StudentApp.Web.Models.Entities;

public class PresentationStudent
{
    public int Id { get; set; }
    public int TaskItemId { get; set; }
    public int StudentId { get; set; }

    public TaskItem TaskItem { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
