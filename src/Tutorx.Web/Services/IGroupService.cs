using Tutorx.Web.Models.Entities;
using Tutorx.Web.Models.ViewModels;

namespace Tutorx.Web.Services;

public record GroupListItem(int Id, string Name);

public interface IGroupService
{
    Task<List<GroupListItem>> GetNonArchivedGroupsAsync();
    Task<List<GroupSummaryVm>> GetGroupSummariesAsync(bool showArchived);
    Task<Group?> GetGroupByIdAsync(int id);
    Task<string?> GetGroupNameAsync(int groupId);
    Task<bool> GroupNameExistsAsync(string name, int? excludeId = null);
    Task<Group> CreateGroupAsync(string name, string? description);
    Task<Group?> UpdateGroupAsync(int id, string name, string? description);
    Task<GroupDetailsVm?> GetGroupDetailsAsync(int id);
    Task<bool> DeleteGroupAsync(int id);
    Task<(bool Success, string Message)> ArchiveGroupAsync(int id);
    Task<bool> UnarchiveGroupAsync(int id);
    Task<bool> GroupExistsAsync(int id);
}
