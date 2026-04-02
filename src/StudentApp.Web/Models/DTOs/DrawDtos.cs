namespace StudentApp.Web.Models.DTOs;

public record DrawResultDto(int StudentId, string FullName, int CycleNumber, int RemainingInBag);
public record DrawHistoryDto(string FullName, DateTime DrawnAt, int CycleNumber);
public record DrawBatchDto(string? ActivityName, string? PresentationTitle, List<string> StudentNames, DateTime DrawnAt);
public record BagStatusDto(int Remaining, int Total, int CurrentCycle);
