using StudentApp.Web.Models.Entities;
using StudentApp.Web.Models.ViewModels;

namespace StudentApp.Web.Services;

public interface IEvaluationService
{
    Task<EvaluationIndexVm?> GetEvaluationIndexAsync(int groupId);
    Task<int?> GetExistingEvaluationIdAsync(int studentId, int taskItemId);
    Task<(string StudentName, string TaskName)?> GetStudentAndTaskInfoAsync(int studentId, int taskItemId);
    Task<Evaluation> CreateEvaluationAsync(int studentId, int taskItemId, decimal score, string? comment);
    Task<EvaluationEditVm?> GetEvaluationForEditAsync(int id);
    Task<bool> UpdateEvaluationAsync(int id, decimal score, string? comment);
    Task<int?> GetStudentGroupIdAsync(int studentId);
}
