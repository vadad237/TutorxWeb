using System.ComponentModel.DataAnnotations;

namespace StudentApp.Web.Models.Entities;

public class TaskItem
{
    public int Id { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = null!;

    public DateTime? PresentationDate { get; set; }
    public bool IsPresentation { get; set; } = false;
    public bool IsNumberedTask { get; set; } = false;
    public decimal? MaxScore { get; set; }
    public int ActivityId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Activity Activity { get; set; } = null!;
    public ICollection<Assignment> Assignments { get; set; } = [];
    public ICollection<Evaluation> Evaluations { get; set; } = [];
    public ICollection<PresentationStudent> PresentationStudents { get; set; } = [];
}
