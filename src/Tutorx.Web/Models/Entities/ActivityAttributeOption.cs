using System.ComponentModel.DataAnnotations;

namespace Tutorx.Web.Models.Entities;

public class ActivityAttributeOption
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(50)]
    public string Color { get; set; } = "secondary";

    public int ActivityAttributeId { get; set; }

    public ActivityAttribute ActivityAttribute { get; set; } = null!;
}
