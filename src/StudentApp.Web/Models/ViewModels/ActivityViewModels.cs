using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StudentApp.Web.Models.ViewModels;

public class ActivitySummaryVm
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string GroupName { get; set; } = null!;
    public int GroupId { get; set; }
    public int TaskCount { get; set; }
    public int AssignedCount { get; set; }
    public bool IsArchived { get; set; }
    public List<string> AssignedStudents { get; set; } = [];
}

public class ActivityCreateVm
{
    [Required(ErrorMessage = "Activity name is required.")]
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

    [Required(ErrorMessage = "Activity name is required.")]
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

public record SimpleTaskVm(int TaskId, string Title);

public record PresentationStudentVm(int StudentId, string FullName);
public record TaskWithAssignmentVm(int TaskId, string Title, DateTime? PresentationDate, List<PresentationStudentVm> PresentationStudents);

public class TaskCreateVm
{
    [Required(ErrorMessage = "Task title is required.")]
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

public class DrawResultVm
{
    public int ActivityId { get; set; }
    public string ActivityName { get; set; } = null!;
    public List<string> DrawnStudentNames { get; set; } = [];
    public List<string> AllStudentNames { get; set; } = [];
}
