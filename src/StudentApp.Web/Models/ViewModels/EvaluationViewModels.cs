using System.ComponentModel.DataAnnotations;

namespace StudentApp.Web.Models.ViewModels;

public class TaskSummaryForEvalVm
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public int ActivityId { get; set; }
    public string ActivityName { get; set; } = null!;
    public bool IsPresentation { get; set; }
}

public class EvaluationIndexVm
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public List<StudentSummaryVm> Students { get; set; } = [];
    public List<TaskSummaryForEvalVm> Tasks { get; set; } = [];
    public Dictionary<(int StudentId, int TaskItemId), decimal> Scores { get; set; } = [];
    public Dictionary<(int StudentId, int TaskItemId), int> EvaluationIds { get; set; } = [];
    public Dictionary<int, decimal> StudentAverages { get; set; } = [];
    public Dictionary<int, decimal> TaskAverages { get; set; } = [];
    public Dictionary<int, decimal> TaskSums { get; set; } = [];
    public Dictionary<(int StudentId, int ActivityId), decimal> ActivityStudentSums { get; set; } = [];
}

public class EvaluationCreateVm
{
    public int StudentId { get; set; }
    public int TaskItemId { get; set; }

    public string StudentName { get; set; } = null!;
    public string TaskName { get; set; } = null!;

    [Required]
    public decimal Score { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }
}

public class EvaluationEditVm
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int TaskItemId { get; set; }

    public string StudentName { get; set; } = null!;
    public string TaskName { get; set; } = null!;

    [Required]
    public decimal Score { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }
}

public class EvaluationItemVm
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public string ActivityName { get; set; } = null!;
    public string TaskName { get; set; } = null!;
    public decimal Score { get; set; }
    public string? Comment { get; set; }
    public DateTime EvaluatedAt { get; set; }
}
