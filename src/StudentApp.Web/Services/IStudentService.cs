using StudentApp.Web.Models.Entities;
using StudentApp.Web.Models.ViewModels;

namespace StudentApp.Web.Services;

public interface IStudentService
{
    Task<List<StudentSummaryVm>> GetStudentsByGroupAsync(int groupId);
    Task<Student> CreateStudentAsync(string firstName, string lastName, string? email, string? cardNumber, int? year, int groupId);
    Task<Student?> GetStudentWithGroupAsync(int id);
    Task<Student?> UpdateStudentAsync(int id, string firstName, string lastName, string? email, string? cardNumber, int? year, bool isActive);
    Task<StudentDetailsVm?> GetStudentDetailsAsync(int id);
    Task<bool> DeleteStudentAsync(int id);
    Task BulkDeleteStudentsAsync(List<int> ids);
    Task<Student?> ToggleActiveAsync(int id);
    Task BulkSetActiveAsync(List<int> ids, bool active);
    Task<List<StudentSummaryVm>> GetActiveStudentsByGroupAsync(int groupId);
}
