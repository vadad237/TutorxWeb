using Tutorx.Web.Models.DTOs;
using Tutorx.Web.Models.Entities;

namespace Tutorx.Web.Services;

public interface IAssignmentService
{
    Task<AssignmentResultDto> AssignTasksAsync(int activityId);
    Task<AssignmentResultDto> AssignStudentsAsync(int activityId);
    Task BulkAssignAsync(int[] activityIds);
    Task<List<Student>> DrawForActivityAsync(int activityId, int count);
    Task<List<Student>> DrawAddForActivityAsync(int activityId, int count, bool includeAlreadyAssigned = false, List<int>? allowedStudentIds = null);
    Task<(List<Student> Students, int BatchId)> DrawAddForPresentationAsync(int taskId, int count, PresentationRole role, bool includeAlreadyAssigned = false, List<int>? allowedStudentIds = null, int? batchId = null);
}
