using System.ComponentModel.DataAnnotations;

namespace Tutorx.Web.Models.Entities;

public class Assignment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int ActivityId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public int? TaskItemId { get; set; }

    public Student Student { get; set; } = null!;
    public Activity Activity { get; set; } = null!;
    public TaskItem? TaskItem { get; set; }
}
