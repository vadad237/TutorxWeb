using StudentApp.Web.Models.Entities;

namespace StudentApp.Web.Services;

public interface ITaskService
{
    Task<TaskItem> CreateTaskAsync(string title, int activityId, DateTime? presentationDate, bool isPresentation);
    Task<(bool Success, string? Message)> SetTitleAsync(int id, string title);
    Task<(bool Success, string? Message)> SetDateAsync(int id, DateTime? presentationDate);
    Task SetPresentationStudentsAsync(int taskId, int[]? studentIds);
    Task SetPresentationStudentsByRoleAsync(int taskId, int[]? studentIds, PresentationRole role);
    Task<(bool Found, int? ActivityId)> DeleteTaskAsync(int id);
    Task<List<EligibleStudentDto>?> GetEligiblePresentationStudentsAsync(int taskId, bool includeAlreadyAssigned, PresentationRole? role = null);
}
