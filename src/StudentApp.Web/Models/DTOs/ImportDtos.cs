namespace StudentApp.Web.Models.DTOs;

public enum ImportRowStatus { Valid, Duplicate, Error }

public record ImportRowDto(
    string FirstName,
    string LastName,
    string? Email,
    string? CardNumber,
    int? Year,
    ImportRowStatus Status,
    string? ErrorMessage
);

public record ImportPreviewDto(List<ImportRowDto> Rows, int GroupId, string GroupName);
