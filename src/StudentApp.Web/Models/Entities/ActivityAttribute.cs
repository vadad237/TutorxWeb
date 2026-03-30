using System.ComponentModel.DataAnnotations;

namespace StudentApp.Web.Models.Entities;

public class ActivityAttribute
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    public int ActivityId { get; set; }

    public Activity Activity { get; set; } = null!;
    public ICollection<ActivityAttributeOption> Options { get; set; } = [];
    public ICollection<StudentAttributeValue> StudentValues { get; set; } = [];
}
