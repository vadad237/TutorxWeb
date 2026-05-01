using Tutorx.Web.Models.ViewModels;

namespace Tutorx.Web.Services;

public interface ICustomExportService
{
    Task<byte[]> GenerateAsync(CustomExportRequestVm request);
}
