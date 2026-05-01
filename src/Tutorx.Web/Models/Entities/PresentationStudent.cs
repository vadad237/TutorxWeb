namespace Tutorx.Web.Models.Entities;

public enum PresentationRole : byte
{
    Presentee    = 0,
    Substitution = 1
}

public class PresentationStudent
{
    public int Id { get; set; }
    public int TaskItemId { get; set; }
    public int StudentId { get; set; }
    public PresentationRole Role { get; set; } = PresentationRole.Presentee;

    public TaskItem TaskItem { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
