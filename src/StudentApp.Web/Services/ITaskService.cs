using StudentApp.Web.Models.Entities;

namespace StudentApp.Web.Services;

public interface ITaskService
{
    Task<TaskItem> CreateTaskAsync(string title, int activityId, DateTime? presentationDate, bool isPresentation, decimal? maxScore = null);
    Task<List<TaskItem>> CreateNumberedTasksAsync(int activityId, int count);
    Task<(bool Success, string? Message)> SetTitleAsync(int id, string title);
    Task<(bool Success, string? Message)> SetDateAsync(int id, DateTime? presentationDate);
    Task<(bool Success, string? Message)> SetMaxScoreAsync(int id, decimal? maxScore);
    Task SetPresentationStudentsAsync(int taskId, int[]? studentIds);
    Task SetPresentationStudentsByRoleAsync(int taskId, int[]? studentIds, PresentationRole role);
    Task<(bool Found, int? ActivityId)> DeleteTaskAsync(int id);
    Task BulkDeleteTasksAsync(int[] ids);
    Task<List<EligibleStudentDto>?> GetEligiblePresentationStudentsAsync(int taskId, bool includeAlreadyAssigned, PresentationRole? role = null);
    Task<(bool Success, string Message)> AutoAssignNumberedTasksAsync(int activityId, int[] taskIds);
}
