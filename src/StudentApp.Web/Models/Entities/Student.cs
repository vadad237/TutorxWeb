using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentApp.Web.Models.Entities;

public class Student
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = null!;

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? CardNumber { get; set; }

    public int? Year { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int GroupId { get; set; }

    public Group Group { get; set; } = null!;
    public ICollection<Assignment> Assignments { get; set; } = [];
    public ICollection<DrawHistory> DrawHistories { get; set; } = [];
    public ICollection<Attendance> Attendances { get; set; } = [];
    public ICollection<Evaluation> Evaluations { get; set; } = [];
    public ICollection<PresentationStudent> PresentationStudents { get; set; } = [];
    public ICollection<StudentAttributeValue> AttributeValues { get; set; } = [];

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
}
