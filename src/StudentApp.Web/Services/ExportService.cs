using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;

namespace StudentApp.Web.Services;

public class ExportService : IExportService
{
    private readonly AppDbContext _db;

    public ExportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> ExportStudentsCsvAsync(int groupId)
    {
        var students = await GetStudentsForExport(groupId);
        using var ms = new MemoryStream();
        // UTF-8 BOM
        ms.Write(Encoding.UTF8.GetPreamble());
        using var writer = new StreamWriter(ms, new UTF8Encoding(false), leaveOpen: true);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        csv.WriteField("Meno"); csv.WriteField("Email"); csv.WriteField("Aktívny");
        csv.WriteField("Počet absencií"); csv.WriteField("Priemerné skóre");
        await csv.NextRecordAsync();
        foreach (var s in students)
        {
            csv.WriteField(s.FullName); csv.WriteField(s.Email ?? "");
            csv.WriteField(s.IsActive ? "Áno" : "Nie");
            csv.WriteField(s.Attendances.Count(a => a.Status == AttendanceStatus.Absent));
            var scores = s.Evaluations.Select(e => e.Score).ToList();
            csv.WriteField(scores.Count > 0 ? scores.Average().ToString("F2") : "-");
            await csv.NextRecordAsync();
        }
        await writer.FlushAsync();
        return ms.ToArray();
    }

    public async Task<byte[]> ExportStudentsXlsxAsync(int groupId)
    {
        var students = await GetStudentsForExport(groupId);
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Študenti");
        ws.Cell(1, 1).Value = "Meno"; ws.Cell(1, 2).Value = "Email";
        ws.Cell(1, 3).Value = "Aktívny"; ws.Cell(1, 4).Value = "Počet absencií";
        ws.Cell(1, 5).Value = "Priemerné skóre";
        var headerRange = ws.Range(1, 1, 1, 5);
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Font.Bold = true;

        int row = 2;
        foreach (var s in students)
        {
            ws.Cell(row, 1).Value = s.FullName;
            ws.Cell(row, 2).Value = s.Email ?? "";
            ws.Cell(row, 3).Value = s.IsActive ? "Áno" : "Nie";
            ws.Cell(row, 4).Value = s.Attendances.Count(a => a.Status == AttendanceStatus.Absent);
            var scores = s.Evaluations.Select(e => (double)e.Score).ToList();
            ws.Cell(row, 5).Value = scores.Count > 0 ? scores.Average().ToString("F1") : "-";
            row++;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public Task<byte[]> ExportStudentsPdfAsync(int groupId)
    {
        // PDF generation will be implemented in Phase 6
        return Task.FromResult(Array.Empty<byte>());
    }

    public async Task<byte[]> ExportAttendanceCsvAsync(int groupId, DateOnly? from, DateOnly? to, string sections = "details")
    {
        var (students, dates, attendanceMap) = await GetAttendanceData(groupId, from, to);
        using var ms = new MemoryStream();
        ms.Write(Encoding.UTF8.GetPreamble());
        using var writer = new StreamWriter(ms, new UTF8Encoding(false), leaveOpen: true);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        bool includeDetails = sections != "summary";
        bool includeSummary = sections != "details";

        csv.WriteField("Študent");
        if (includeDetails)
            foreach (var d in dates) csv.WriteField(d.ToString("dd.MM.yyyy"));
        if (includeSummary)
        {
            csv.WriteField("Prítomný");
            csv.WriteField("Neprítomný");
            csv.WriteField("Ospravedlnený");
            csv.WriteField("Celkom");
            csv.WriteField("Dochádzka %");
        }
        await csv.NextRecordAsync();
        foreach (var s in students)
        {
            csv.WriteField(s.FullName);
            var present = dates.Count(d => attendanceMap.TryGetValue((s.Id, d), out var st) && st == AttendanceStatus.Present);
            var absent  = dates.Count(d => attendanceMap.TryGetValue((s.Id, d), out var st) && st == AttendanceStatus.Absent);
            var excused = dates.Count(d => attendanceMap.TryGetValue((s.Id, d), out var st) && st == AttendanceStatus.Excused);
            var total   = present + absent + excused;
            var pct     = total > 0 ? Math.Round((double)present / total * 100, 1) : 0.0;
            if (includeDetails)
                foreach (var d in dates)
                    csv.WriteField(attendanceMap.TryGetValue((s.Id, d), out var status) ? status.ToString() : "-");
            if (includeSummary)
            {
                csv.WriteField(present);
                csv.WriteField(absent);
                csv.WriteField(excused);
                csv.WriteField(total);
                csv.WriteField(pct.ToString("F1") + "%");
            }
            await csv.NextRecordAsync();
        }
        await writer.FlushAsync();
        return ms.ToArray();
    }

    public async Task<byte[]> ExportAttendanceXlsxAsync(int groupId, DateOnly? from, DateOnly? to, string sections = "details")
    {
        var (students, dates, attendanceMap) = await GetAttendanceData(groupId, from, to);
        using var workbook = new XLWorkbook();

        bool includeDetails = sections != "summary";
        bool includeSummary = sections != "details";

        if (includeDetails)
        {
            var ws = workbook.Worksheets.Add("Dochádzka – Detail");
            ws.Cell(1, 1).Value = "Študent";
            for (int i = 0; i < dates.Count; i++)
                ws.Cell(1, i + 2).Value = dates[i].ToString("dd.MM.yyyy");
            var detailHeaderRange = ws.Range(1, 1, 1, Math.Max(1, dates.Count + 1));
            detailHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            detailHeaderRange.Style.Font.FontColor = XLColor.White;
            detailHeaderRange.Style.Font.Bold = true;
            int row = 2;
            foreach (var s in students)
            {
                ws.Cell(row, 1).Value = s.FullName;
                for (int i = 0; i < dates.Count; i++)
                    ws.Cell(row, i + 2).Value = attendanceMap.TryGetValue((s.Id, dates[i]), out var st) ? st.ToString() : "-";
                row++;
            }
            ws.Columns().AdjustToContents();
        }

        if (includeSummary)
        {
            var ws = workbook.Worksheets.Add("Dochádzka – Súhrn");
            ws.Cell(1, 1).Value = "Študent";
            ws.Cell(1, 2).Value = "Prítomný";
            ws.Cell(1, 3).Value = "Neprítomný";
            ws.Cell(1, 4).Value = "Ospravedlnený";
            ws.Cell(1, 5).Value = "Celkom";
            ws.Cell(1, 6).Value = "Dochádzka %";
            var summaryHeaderRange = ws.Range(1, 1, 1, 6);
            summaryHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");
            summaryHeaderRange.Style.Font.FontColor = XLColor.White;
            summaryHeaderRange.Style.Font.Bold = true;
            int row = 2;
            foreach (var s in students)
            {
                var present = dates.Count(d => attendanceMap.TryGetValue((s.Id, d), out var st) && st == AttendanceStatus.Present);
                var absent  = dates.Count(d => attendanceMap.TryGetValue((s.Id, d), out var st) && st == AttendanceStatus.Absent);
                var excused = dates.Count(d => attendanceMap.TryGetValue((s.Id, d), out var st) && st == AttendanceStatus.Excused);
                var total   = present + absent + excused;
                var pct     = total > 0 ? Math.Round((double)present / total * 100, 1) : 0.0;
                ws.Cell(row, 1).Value = s.FullName;
                ws.Cell(row, 2).Value = present;
                ws.Cell(row, 3).Value = absent;
                ws.Cell(row, 4).Value = excused;
                ws.Cell(row, 5).Value = total;
                var pctCell = ws.Cell(row, 6);
                pctCell.Value = pct / 100;
                pctCell.Style.NumberFormat.Format = "0.0%";
                pctCell.Style.Fill.BackgroundColor = pct >= 80
                    ? XLColor.FromHtml("#C8E6C9")
                    : pct >= 50 ? XLColor.FromHtml("#FFF9C4") : XLColor.FromHtml("#FFCDD2");
                row++;
            }
            ws.Columns().AdjustToContents();
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public Task<byte[]> ExportAttendancePdfAsync(int groupId, DateOnly? from, DateOnly? to)
    {
        return Task.FromResult(Array.Empty<byte>());
    }

    public async Task<byte[]> ExportEvaluationsCsvAsync(int groupId, int? activityId, string sections = "details")
    {
        bool includeDetails = sections != "summary";
        bool includeSummary = sections != "details";

        var evaluations = await GetEvaluationsData(groupId, activityId);
        using var ms = new MemoryStream();
        ms.Write(Encoding.UTF8.GetPreamble());
        using var writer = new StreamWriter(ms, new UTF8Encoding(false), leaveOpen: true);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        if (includeDetails)
        {
            csv.WriteField("Študent"); csv.WriteField("Úloha"); csv.WriteField("Skóre"); csv.WriteField("Komentár");
            await csv.NextRecordAsync();
            foreach (var e in evaluations)
            {
                csv.WriteField(e.Student.FullName); csv.WriteField(e.TaskItem.Title);
                csv.WriteField(e.Score.ToString("F1")); csv.WriteField(e.Comment ?? "");
                await csv.NextRecordAsync();
            }
        }

        if (includeSummary)
        {
            if (includeDetails)
            {
                await csv.NextRecordAsync(); // blank separator row
                csv.WriteField("--- Súhrn ---");
                await csv.NextRecordAsync();
            }
            var activities = evaluations.Select(e => new { e.TaskItem.ActivityId, e.TaskItem.Activity!.Name })
                .DistinctBy(a => a.ActivityId).ToList();
            csv.WriteField("Študent");
            foreach (var a in activities) csv.WriteField(a.Name + " (súčet)");
            csv.WriteField("Celkom");
            await csv.NextRecordAsync();
            var byStudent = evaluations.GroupBy(e => e.Student).OrderBy(g => g.Key.LastName).ThenBy(g => g.Key.FirstName);
            foreach (var sg in byStudent)
            {
                csv.WriteField(sg.Key.FullName);
                foreach (var a in activities)
                {
                    var sum = sg.Where(e => e.TaskItem.ActivityId == a.ActivityId).Sum(e => e.Score);
                    csv.WriteField(sum > 0 ? sum.ToString("F1") : "-");
                }
                csv.WriteField(sg.Sum(e => e.Score).ToString("F1"));
                await csv.NextRecordAsync();
            }
        }

        await writer.FlushAsync();
        return ms.ToArray();
    }

    public async Task<byte[]> ExportEvaluationsXlsxAsync(int groupId, int? activityId, string sections = "details")
    {
        bool includeDetails = sections != "summary";
        bool includeSummary = sections != "details";

        var evaluations = await GetEvaluationsData(groupId, activityId);
        using var workbook = new XLWorkbook();

        if (includeDetails)
        {
            var ws = workbook.Worksheets.Add("Hodnotenia – Detail");
            ws.Cell(1, 1).Value = "Študent"; ws.Cell(1, 2).Value = "Úloha";
            ws.Cell(1, 3).Value = "Skóre"; ws.Cell(1, 4).Value = "Komentár";
            var hdr = ws.Range(1, 1, 1, 4);
            hdr.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            hdr.Style.Font.FontColor = XLColor.White;
            hdr.Style.Font.Bold = true;
            int row = 2;
            foreach (var e in evaluations)
            {
                ws.Cell(row, 1).Value = e.Student.FullName;
                ws.Cell(row, 2).Value = e.TaskItem.Title;
                ws.Cell(row, 3).Value = (double)e.Score;
                ws.Cell(row, 4).Value = e.Comment ?? "";
                row++;
            }
            ws.Columns().AdjustToContents();
        }

        if (includeSummary)
        {
            var activities = evaluations.Select(e => new { e.TaskItem.ActivityId, e.TaskItem.Activity!.Name })
                .DistinctBy(a => a.ActivityId).ToList();
            var ws = workbook.Worksheets.Add("Hodnotenia – Súhrn");
            ws.Cell(1, 1).Value = "Študent";
            for (int i = 0; i < activities.Count; i++)
                ws.Cell(1, i + 2).Value = activities[i].Name + " (súčet)";
            ws.Cell(1, activities.Count + 2).Value = "Celkom";
            int totalCols = activities.Count + 2;
            var hdr = ws.Range(1, 1, 1, totalCols);
            hdr.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            hdr.Style.Font.FontColor = XLColor.White;
            hdr.Style.Font.Bold = true;
            int row = 2;
            var byStudent = evaluations.GroupBy(e => e.Student).OrderBy(g => g.Key.LastName).ThenBy(g => g.Key.FirstName);
            foreach (var sg in byStudent)
            {
                ws.Cell(row, 1).Value = sg.Key.FullName;
                for (int i = 0; i < activities.Count; i++)
                {
                    var sum = sg.Where(e => e.TaskItem.ActivityId == activities[i].ActivityId).Sum(e => (double)e.Score);
                    ws.Cell(row, i + 2).Value = sum;
                }
                ws.Cell(row, activities.Count + 2).Value = (double)sg.Sum(e => e.Score);
                row++;
            }
            ws.Columns().AdjustToContents();
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public Task<byte[]> ExportEvaluationsPdfAsync(int groupId, int? activityId)
    {
        return Task.FromResult(Array.Empty<byte>());
    }

    public async Task<byte[]> ExportAssignmentsCsvAsync(int activityId)
    {
        var assignments = await GetAssignmentsData(activityId);
        using var ms = new MemoryStream();
        ms.Write(Encoding.UTF8.GetPreamble());
        using var writer = new StreamWriter(ms, new UTF8Encoding(false), leaveOpen: true);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        csv.WriteField("Študent"); csv.WriteField("Skóre");
        await csv.NextRecordAsync();
        foreach (var a in assignments)
        {
            csv.WriteField(a.Student.FullName);
            var eval = a.TaskItemId.HasValue
                ? await _db.Evaluations.FirstOrDefaultAsync(e => e.StudentId == a.StudentId && e.TaskItemId == a.TaskItemId)
                : null;
            csv.WriteField(eval != null ? eval.Score.ToString("F1") : "-");
            await csv.NextRecordAsync();
        }
        await writer.FlushAsync();
        return ms.ToArray();
    }

    public async Task<byte[]> ExportAssignmentsXlsxAsync(int activityId)
    {
        var assignments = await GetAssignmentsData(activityId);
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Priradenia");
        ws.Cell(1, 1).Value = "Študent"; ws.Cell(1, 2).Value = "Skóre";
        var headerRange = ws.Range(1, 1, 1, 2);
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Font.Bold = true;

        int row = 2;
        foreach (var a in assignments)
        {
            ws.Cell(row, 1).Value = a.Student.FullName;
            var eval = a.TaskItemId.HasValue
                ? await _db.Evaluations.FirstOrDefaultAsync(e => e.StudentId == a.StudentId && e.TaskItemId == a.TaskItemId)
                : null;
            ws.Cell(row, 2).Value = eval != null ? eval.Score.ToString("F2") : "-";
            row++;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public Task<byte[]> ExportAssignmentsPdfAsync(int activityId)
    {
        return Task.FromResult(Array.Empty<byte>());
    }

    private async Task<List<Student>> GetStudentsForExport(int groupId)
    {
        return await _db.Students
            .Where(s => s.GroupId == groupId)
            .Include(s => s.Attendances)
            .Include(s => s.Evaluations)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync();
    }

    private async Task<(List<Student> Students, List<DateOnly> Dates, Dictionary<(int, DateOnly), AttendanceStatus> Map)> GetAttendanceData(int groupId, DateOnly? from, DateOnly? to)
    {
        var students = await _db.Students
            .Where(s => s.GroupId == groupId && s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync();

        var query = _db.Attendances.Where(a => a.GroupId == groupId);
        if (from.HasValue) query = query.Where(a => a.Date >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Date <= to.Value);
        var attendances = await query.ToListAsync();

        var dates = attendances.Select(a => a.Date).Distinct().OrderBy(d => d).ToList();
        var map = attendances.ToDictionary(a => (a.StudentId, a.Date), a => a.Status);

        return (students, dates, map);
    }

    private async Task<List<Evaluation>> GetEvaluationsData(int groupId, int? activityId)
    {
        var query = _db.Evaluations
            .Include(e => e.Student)
            .Include(e => e.TaskItem)
                .ThenInclude(t => t.Activity)
            .Where(e => e.Student.GroupId == groupId);

        if (activityId.HasValue)
            query = query.Where(e => e.TaskItem.ActivityId == activityId.Value);

        return await query.OrderBy(e => e.Student.LastName).ThenBy(e => e.Student.FirstName).ToListAsync();
    }

    private async Task<List<Assignment>> GetAssignmentsData(int activityId)
    {
        return await _db.Assignments
            .Where(a => a.ActivityId == activityId)
            .Include(a => a.Student)
            .OrderBy(a => a.Student.LastName).ThenBy(a => a.Student.FirstName)
            .ToListAsync();
    }
}
