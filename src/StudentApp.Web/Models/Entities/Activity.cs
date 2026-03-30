using System.ComponentModel.DataAnnotations;

namespace StudentApp.Web.Models.Entities;

public class Activity
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int GroupId { get; set; }

    public Group Group { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = [];
    public ICollection<Assignment> Assignments { get; set; } = [];
    public ICollection<ActivityAttribute> OtherAttributes { get; set; } = [];
}
