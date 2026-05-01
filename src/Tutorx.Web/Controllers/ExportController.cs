using Microsoft.AspNetCore.Mvc;
using Tutorx.Web.Services;

namespace Tutorx.Web.Controllers;

public class ExportController : Controller
{
    private readonly IExportService _exportService;

    public ExportController(IExportService exportService)
    {
        _exportService = exportService;
    }

    [HttpGet]
    public async Task<IActionResult> Students(int groupId, string format = "xlsx")
    {
        byte[] data;
        string contentType;
        string fileName;

        switch (format.ToLower())
        {
            case "csv":
                data = await _exportService.ExportStudentsCsvAsync(groupId);
                contentType = "text/csv";
                fileName = "students.csv";
                break;
            case "pdf":
                data = await _exportService.ExportStudentsPdfAsync(groupId);
                contentType = "application/pdf";
                fileName = "students.pdf";
                break;
            default:
                data = await _exportService.ExportStudentsXlsxAsync(groupId);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = "students.xlsx";
                break;
        }

        if (data.Length == 0)
            return BadRequest("Export format not available.");

        return File(data, contentType, fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Attendance(int groupId, DateOnly? from = null, DateOnly? to = null, string format = "xlsx", string sections = "details")
    {
        byte[] data;
        string contentType;
        string fileName;

        switch (format.ToLower())
        {
            case "csv":
                data = await _exportService.ExportAttendanceCsvAsync(groupId, from, to, sections);
                contentType = "text/csv";
                fileName = "attendance.csv";
                break;
            case "pdf":
                data = await _exportService.ExportAttendancePdfAsync(groupId, from, to);
                contentType = "application/pdf";
                fileName = "attendance.pdf";
                break;
            default:
                data = await _exportService.ExportAttendanceXlsxAsync(groupId, from, to, sections);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = "attendance.xlsx";
                break;
        }

        if (data.Length == 0)
            return BadRequest("Export format not available.");

        return File(data, contentType, fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Evaluations(int groupId, int? activityId = null, string format = "xlsx", string sections = "details")
    {
        byte[] data;
        string contentType;
        string fileName;

        switch (format.ToLower())
        {
            case "csv":
                data = await _exportService.ExportEvaluationsCsvAsync(groupId, activityId, sections);
                contentType = "text/csv";
                fileName = "evaluations.csv";
                break;
            case "pdf":
                data = await _exportService.ExportEvaluationsPdfAsync(groupId, activityId);
                contentType = "application/pdf";
                fileName = "evaluations.pdf";
                break;
            default:
                data = await _exportService.ExportEvaluationsXlsxAsync(groupId, activityId, sections);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = "evaluations.xlsx";
                break;
        }

        if (data.Length == 0)
            return BadRequest("Export format not available.");

        return File(data, contentType, fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Assignments(int activityId, string format = "xlsx")
    {
        byte[] data;
        string contentType;
        string fileName;

        switch (format.ToLower())
        {
            case "csv":
                data = await _exportService.ExportAssignmentsCsvAsync(activityId);
                contentType = "text/csv";
                fileName = "assignments.csv";
                break;
            case "pdf":
                data = await _exportService.ExportAssignmentsPdfAsync(activityId);
                contentType = "application/pdf";
                fileName = "assignments.pdf";
                break;
            default:
                data = await _exportService.ExportAssignmentsXlsxAsync(activityId);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = "assignments.xlsx";
                break;
        }

        if (data.Length == 0)
            return BadRequest("Export format not available.");

        return File(data, contentType, fileName);
    }
}
