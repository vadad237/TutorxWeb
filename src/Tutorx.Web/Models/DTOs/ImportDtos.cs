namespace Tutorx.Web.Models.DTOs;

public enum ImportRowStatus { Valid, Error }

public record ImportRowDto(
    string FirstName,
    string LastName,
    string? Email,
    string? CardNumber,
    int? Year,
    string? GroupNumber,
    ImportRowStatus Status,
    string? ErrorMessage
);

public record ImportPreviewDto(List<ImportRowDto> Rows, int GroupId, string GroupName);
