using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tutorx.Web.Models.ViewModels;

public class ActivitySummaryVm
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string GroupName { get; set; } = null!;
    public int GroupId { get; set; }
    public int TaskCount { get; set; }
    public int NumberedTaskCount { get; set; }
    public int OrdinaryTaskCount { get; set; }
    public int PresentationCount { get; set; }
    public int AssignedCount { get; set; }
    public bool IsArchived { get; set; }
    public List<string> AssignedStudents { get; set; } = [];
    public List<(int Id, string Name)> AssignedStudentDetails { get; set; } = [];
    public HashSet<int> StudentsWithNumberedTask { get; set; } = [];
}

public class ActivityCreateVm
{
    [Required(ErrorMessage = "Názov aktivity je povinný.")]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int GroupId { get; set; }

    [BindNever]
    public string GroupName { get; set; } = null!;
}

public class ActivityEditVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Názov aktivity je povinný.")]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int GroupId { get; set; }
}

public class ActivityDetailsVm
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public bool IsArchived { get; set; }
    public List<SimpleTaskVm> Tasks { get; set; } = [];
    public List<NumberedTaskVm> NumberedTasks { get; set; } = [];
    public List<TaskWithAssignmentVm> Presentations { get; set; } = [];
    public List<AssignedStudentVm> AssignedStudents { get; set; } = [];
    public List<OtherAttributeVm> OtherAttributes { get; set; } = [];
    public List<StudentAttributeValueVm> AttributeValues { get; set; } = [];
    public bool HasAssignments => AssignedStudents.Any();
    public int UnassignedStudentCount { get; set; }
    public int UnassignedTaskCount { get; set; }
}

public record AssignedStudentVm(int AssignmentId, int StudentId, string FullName);

public record AttributeOptionVm(int OptionId, string Name, string Color);
public record OtherAttributeVm(int AttributeId, string Name, List<AttributeOptionVm> Options);
public record StudentAttributeValueVm(int StudentId, int AttributeId, int? OptionId, string? OptionName, string? OptionColor);

public record SimpleTaskVm(int TaskId, string Title, DateTime CreatedAt, decimal? MaxScore);
public record NumberedTaskVm(int TaskId, int Number, List<PresentationStudentVm> AssignedStudents);

public record PresentationStudentVm(int StudentId, string FullName);
public record TaskWithAssignmentVm(int TaskId, string Title, DateTime? PresentationDate,
    List<PresentationStudentVm> PresenteeStudents,
    List<PresentationStudentVm> SubstitutionStudents,
    decimal? MaxScore);

public class TaskCreateVm
{
    [Required(ErrorMessage = "Názov položky je povinný.")]
    [MaxLength(300)]
    public string Title { get; set; } = null!;
    public int ActivityId { get; set; }
}

public class ManualAssignVm
{
    public int AssignmentId { get; set; }
    public int ActivityId { get; set; }
    public int TaskItemId { get; set; }
    public int StudentId { get; set; }
    public List<StudentSummaryVm> AvailableStudents { get; set; } = [];
}
