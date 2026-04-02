using StudentApp.Web.Models.DTOs;
using StudentApp.Web.Models.Entities;

namespace StudentApp.Web.Services;

public interface IAssignmentService
{
    Task<AssignmentResultDto> AssignTasksAsync(int activityId);
    Task<AssignmentResultDto> AssignStudentsAsync(int activityId);
    Task BulkAssignAsync(int[] activityIds);
    Task<List<Student>> DrawForActivityAsync(int activityId, int count);
    Task<List<Student>> DrawAddForActivityAsync(int activityId, int count, bool includeAlreadyAssigned = false, List<int>? allowedStudentIds = null);
    Task<List<Student>> DrawAddForPresentationAsync(int taskId, int count, bool includeAlreadyAssigned = false, List<int>? allowedStudentIds = null);
}
