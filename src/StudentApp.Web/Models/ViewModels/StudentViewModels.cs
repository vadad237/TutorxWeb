using System.ComponentModel.DataAnnotations;
using StudentApp.Web.Models.DTOs;

namespace StudentApp.Web.Models.ViewModels;

public class StudentSummaryVm
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? CardNumber { get; set; }
    public int? Year { get; set; }
    public bool IsActive { get; set; }
    public int AbsenceCount { get; set; }
    public decimal? AvgScore { get; set; }
    public int GroupId { get; set; }
}

public class StudentCreateVm
{
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100)]
    public string LastName { get; set; } = null!;

    [MaxLength(200)]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? CardNumber { get; set; }

    [Range(1, 10, ErrorMessage = "Year must be between 1 and 10.")]
    public int? Year { get; set; }

    public int GroupId { get; set; }
}

public class StudentEditVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100)]
    public string LastName { get; set; } = null!;

    [MaxLength(200)]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? CardNumber { get; set; }

    [Range(1, 10, ErrorMessage = "Year must be between 1 and 10.")]
    public int? Year { get; set; }

    public bool IsActive { get; set; }
    public int GroupId { get; set; }
}

public class StudentDetailsVm
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? CardNumber { get; set; }
    public int? Year { get; set; }
    public bool IsActive { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<DrawHistoryDto> DrawHistory { get; set; } = [];
    public AttendanceSummaryItemVm AttendanceSummary { get; set; } = new();
    public List<EvaluationItemVm> Evaluations { get; set; } = [];
    public List<AssignedActivityVm> AssignedActivities { get; set; } = [];
}

public record AssignedActivityVm(int ActivityId, string Name);

public class ImportUploadVm
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = null!;
}

public class ImportPreviewVm
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public List<ImportRowVm> Rows { get; set; } = [];
}

public class ImportRowVm
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public string? CardNumber { get; set; }
    public int? Year { get; set; }
    public string Status { get; set; } = null!; // Valid, Duplicate, Error
    public string? ErrorMessage { get; set; }
    public bool Selected { get; set; }
}

public class ImportConfirmVm
{
    public int GroupId { get; set; }
    public List<ImportRowVm> Rows { get; set; } = [];
}
