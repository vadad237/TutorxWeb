using Tutorx.Web.Models.Entities;
using Tutorx.Web.Models.ViewModels;

namespace Tutorx.Web.Services;

public record EligibleStudentDto(int Id, string FullName);

public interface IActivityService
{
    Task<List<ActivitySummaryVm>> GetActivitySummariesAsync(int groupId);
    Task<(int Id, string Name)?> GetGroupInfoAsync(int groupId);
    Task<Activity> CreateActivityAsync(string name, string? description, int groupId);
    Task<Activity?> GetActivityByIdAsync(int id);
    Task<Activity?> UpdateActivityAsync(int id, string name, string? description);
    Task<(ActivityDetailsVm Details, List<StudentSummaryVm> AllStudents)?> GetActivityDetailsAsync(int id);
    Task<(bool Success, string Message)> ArchiveActivityAsync(int id);
    Task<(bool Exists, bool Deleted)> DeleteActivityAsync(int id);
    Task BulkDeleteActivitiesAsync(List<int> ids);
    Task<List<EligibleStudentDto>?> GetEligibleStudentsAsync(int activityId, bool includeAlreadyAssigned);
    Task<(bool Success, int? NewId, string? Message)> DuplicateActivityAsync(int id);
    Task<bool> SetActivityAssignmentsAsync(int activityId, int[]? studentIds);
    Task<List<DrawActivityVm>> GetActivitiesForGroupAsync(int groupId);
    Task<List<DrawPresentationVm>> GetPresentationsForGroupAsync(int groupId);
    Task<List<string>> GetActiveStudentNamesForGroupAsync(int groupId);
}
