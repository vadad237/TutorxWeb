using System.ComponentModel.DataAnnotations;

namespace Tutorx.Web.Models.ViewModels;

public class GroupSummaryVm
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int StudentCount { get; set; }
    public int ActiveStudentCount { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GroupCreateVm
{
    [Required(ErrorMessage = "Group name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class GroupEditVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Group name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class GroupDetailsVm
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public int InactiveStudents { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<StudentSummaryVm> Students { get; set; } = [];
}
