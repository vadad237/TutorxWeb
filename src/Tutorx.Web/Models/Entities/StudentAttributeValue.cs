namespace Tutorx.Web.Models.Entities;

public class StudentAttributeValue
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public int ActivityAttributeId { get; set; }
    public int? OptionId { get; set; }

    public Student Student { get; set; } = null!;
    public ActivityAttribute ActivityAttribute { get; set; } = null!;
    public ActivityAttributeOption? Option { get; set; }
}
