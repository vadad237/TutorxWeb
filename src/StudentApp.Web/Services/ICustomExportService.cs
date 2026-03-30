using StudentApp.Web.Models.ViewModels;

namespace StudentApp.Web.Services;

public interface ICustomExportService
{
    Task<byte[]> GenerateAsync(CustomExportRequestVm request);
}
