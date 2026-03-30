using StudentApp.Web.Models.DTOs;

namespace StudentApp.Web.Services;

public interface IImportService
{
    Task<ImportPreviewDto> ParseFileAsync(Stream fileStream, string fileName, int groupId);
    Task<int> ImportStudentsAsync(int groupId, List<ImportRowDto> rows);
}
