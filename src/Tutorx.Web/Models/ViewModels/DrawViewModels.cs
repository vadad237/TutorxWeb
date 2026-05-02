using Tutorx.Web.Models.DTOs;

namespace Tutorx.Web.Models.ViewModels;

public class DrawIndexVm
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public List<string> AllActiveStudentNames { get; set; } = [];
    public DrawHistoryDto? LastDraw { get; set; }
    public List<GroupSummaryVm> AllGroups { get; set; } = [];
    public List<DrawActivityVm> Activities { get; set; } = [];
    public List<DrawPresentationVm> Presentations { get; set; } = [];
}

public class DrawActivityVm
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

public class DrawPresentationVm
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public int ActivityId { get; set; }
    public string ActivityName { get; set; } = null!;
}

public class DrawHistoryVm
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public List<DrawHistoryDto> History { get; set; } = [];
    public List<DrawBatchDto> Batches { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}
