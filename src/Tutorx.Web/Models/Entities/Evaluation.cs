using System.ComponentModel.DataAnnotations;

namespace Tutorx.Web.Models.Entities;

public class Evaluation
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int TaskItemId { get; set; }
    public decimal Score { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

    public Student Student { get; set; } = null!;
    public TaskItem TaskItem { get; set; } = null!;
}
