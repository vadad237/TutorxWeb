namespace Tutorx.Web.Models.DTOs;

public record DrawHistoryDto(string FullName, DateTime DrawnAt, int CycleNumber);
public record DrawStudentDto(string Name, byte? PresRole);
public record DrawBatchDto(string? ActivityName, string? PresentationTitle, List<DrawStudentDto> Students, DateTime DrawnAt);
