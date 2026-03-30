namespace StudentApp.Web.Services;

public interface IExportService
{
    Task<byte[]> ExportStudentsCsvAsync(int groupId);
    Task<byte[]> ExportStudentsXlsxAsync(int groupId);
    Task<byte[]> ExportStudentsPdfAsync(int groupId);
    Task<byte[]> ExportAttendanceCsvAsync(int groupId, DateOnly? from, DateOnly? to);
    Task<byte[]> ExportAttendanceXlsxAsync(int groupId, DateOnly? from, DateOnly? to);
    Task<byte[]> ExportAttendancePdfAsync(int groupId, DateOnly? from, DateOnly? to);
    Task<byte[]> ExportEvaluationsCsvAsync(int groupId, int? activityId);
    Task<byte[]> ExportEvaluationsXlsxAsync(int groupId, int? activityId);
    Task<byte[]> ExportEvaluationsPdfAsync(int groupId, int? activityId);
    Task<byte[]> ExportAssignmentsCsvAsync(int activityId);
    Task<byte[]> ExportAssignmentsXlsxAsync(int activityId);
    Task<byte[]> ExportAssignmentsPdfAsync(int activityId);
}
