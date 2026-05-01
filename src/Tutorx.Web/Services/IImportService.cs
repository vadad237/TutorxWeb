using Tutorx.Web.Models.DTOs;

namespace Tutorx.Web.Services;

public interface IImportService
{
    Task<ImportPreviewDto> ParseFileAsync(Stream fileStream, string fileName, int groupId);
    Task<int> ImportStudentsAsync(int groupId, List<ImportRowDto> rows);
}
