using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;
using StudentApp.Web.Models.ViewModels;

namespace StudentApp.Web.Services;

public class CustomExportService : ICustomExportService
{
    private readonly AppDbContext _db;

    public CustomExportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> GenerateAsync(CustomExportRequestVm request)
    {
        // ── 1. Load students ────────────────────────────────────────────────
        var students = await _db.Students
            .Where(s => s.GroupId == request.GroupId && s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync();

        // ── 2. Build headers and load data per section ─────────────────────
        var headers = new List<(string Text, string Section)>();

        // --- Students ---
        if (request.IncludeStudents)
        {
            if (request.IncludeStudentFirstName)  headers.Add(("Meno",        "students"));
            if (request.IncludeStudentLastName)   headers.Add(("Priezvisko",  "students"));
            if (request.IncludeStudentCardNumber) headers.Add(("Číslo karty", "students"));
            if (request.IncludeStudentYear)       headers.Add(("Ročník",      "students"));
            if (request.IncludeStudentEmail)      headers.Add(("Email",       "students"));
        }

        // --- Attendance ---
        // attendanceDateMap[studentId][date] = status label
        List<DateOnly> attendanceDates = [];
        Dictionary<int, Dictionary<DateOnly, string>> attendanceDateMap = new();
        if (request.IncludeAttendance)
        {
            var records = await _db.Attendances
                .Where(a => a.GroupId == request.GroupId)
                .OrderBy(a => a.Date)
                .ToListAsync();

            attendanceDates = records.Select(a => a.Date).Distinct().OrderBy(d => d).ToList();

            attendanceDateMap = records
                .GroupBy(a => a.StudentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(
                        a => a.Date,
                        a => a.Status switch
                        {
                            AttendanceStatus.Present => "P",
                            AttendanceStatus.Absent  => "N",
                            AttendanceStatus.Excused => "O",
                            _ => ""
                        }));

            foreach (var date in attendanceDates)
                if (request.IncludeAttendanceDetails)
                    headers.Add((date.ToString("dd.MM.yyyy"), "attendance"));
            if (request.IncludeAttendanceSummary)
            {
                headers.Add(("Prítomný",    "att-sum"));
                headers.Add(("Neprítomný",  "att-sum"));
                headers.Add(("Ospravedlnený", "att-sum"));
                headers.Add(("Dochádzka %",  "att-sum"));
            }
        }

        // --- Activities ---
        // activityList holds each activity in sorted order; assignedSet[studentId, activityId] = assigned
        List<Activity> activityList = [];
        HashSet<(int studentId, int activityId)> activityAssignedSet = new();
        if (request.IncludeActivities)
        {
            activityList = await _db.Activities
                .Where(a => a.GroupId == request.GroupId && !a.IsArchived)
                .OrderBy(a => a.Name)
                .ToListAsync();

            var assignments = await _db.Assignments
                .Where(a => a.Activity.GroupId == request.GroupId && !a.Activity.IsArchived)
                .Select(a => new { a.StudentId, a.ActivityId })
                .ToListAsync();

            foreach (var asgn in assignments)
                activityAssignedSet.Add((asgn.StudentId, asgn.ActivityId));

            foreach (var activity in activityList)
                headers.Add((activity.Name, "activities"));
        }

        // --- Tasks & Evaluations ---
        List<TaskItem> tasks = new();
        List<TaskItem> taskPresentations = new();
        Dictionary<(int studentId, int taskId), Evaluation> evalMap = new();
        if (request.IncludeTasks)
        {
            tasks = await _db.TaskItems
                .Include(t => t.Activity)
                .Where(t => !t.IsPresentation
                         && t.Activity.GroupId == request.GroupId
                         && !t.Activity.IsArchived)
                .OrderBy(t => t.Activity.Name).ThenBy(t => t.Title)
                .ToListAsync();

            taskPresentations = await _db.TaskItems
                .Include(t => t.Activity)
                .Where(t => t.IsPresentation
                         && t.Activity.GroupId == request.GroupId
                         && !t.Activity.IsArchived)
                .OrderBy(t => t.Activity.Name).ThenBy(t => t.PresentationDate).ThenBy(t => t.Title)
                .ToListAsync();

            var evals = await _db.Evaluations
                .Where(e => e.TaskItem.Activity.GroupId == request.GroupId)
                .ToListAsync();

            evalMap = evals.ToDictionary(e => (e.StudentId, e.TaskItemId));

            // Collect all activity IDs present in either list
            var allActivityIds = tasks.Select(t => t.ActivityId)
                .Union(taskPresentations.Select(t => t.ActivityId))
                .Distinct()
                .OrderBy(id => tasks.FirstOrDefault(t => t.ActivityId == id)?.Activity.Name
                             ?? taskPresentations.First(t => t.ActivityId == id).Activity.Name)
                .ToList();

            foreach (var actId in allActivityIds)
            {
                var actName = tasks.FirstOrDefault(t => t.ActivityId == actId)?.Activity.Name
                           ?? taskPresentations.First(t => t.ActivityId == actId).Activity.Name;

                if (request.IncludeTasksDetails)
                {
                    foreach (var t in tasks.Where(t => t.ActivityId == actId))
                    {
                        var maxSuffix = t.MaxScore.HasValue ? $" (max: {t.MaxScore.Value.ToString("0.##")})" : "";
                        headers.Add(($"{actName} › {t.Title}{maxSuffix}", "tasks"));
                    }
                    foreach (var p in taskPresentations.Where(t => t.ActivityId == actId))
                    {
                        var dateStr = p.PresentationDate.HasValue
                            ? p.PresentationDate.Value.ToString("dd.MM.yyyy")
                            : "bez dátumu";
                        var maxSuffix = p.MaxScore.HasValue ? $" (max: {p.MaxScore.Value.ToString("0.##")})" : "";
                        headers.Add(($"{actName} › {p.Title} ({dateStr}){maxSuffix}", "tasks-pres"));
                    }
                }
                if (request.IncludeTasksSummary)
                {
                    headers.Add(($"{actName} › Celkom", "tasks-sum"));
                }
            }
            if (request.IncludeTasksSummary)
            {
                headers.Add(("Celkom (všetky aktivity)", "tasks-total"));
            }
        }

        // --- Other Attributes ---
        // attrList[i] = (attributeId, "ActivityName › AttrName")
        List<(int AttributeId, string Header)> attrList = [];
        Dictionary<(int studentId, int attributeId), string> attrValueMap = new();
        if (request.IncludeOtherAttributes)
        {
            var attrs = await _db.ActivityAttributes
                .Include(a => a.Activity)
                .Where(a => a.Activity.GroupId == request.GroupId && !a.Activity.IsArchived)
                .OrderBy(a => a.Activity.Name).ThenBy(a => a.Id)
                .ToListAsync();

            var values = await _db.StudentAttributeValues
                .Include(v => v.Option)
                .Where(v => v.ActivityAttribute.Activity.GroupId == request.GroupId)
                .ToListAsync();

            foreach (var attr in attrs)
            {
                attrList.Add((attr.Id, $"{attr.Activity.Name} › {attr.Name}"));
                headers.Add(($"{attr.Activity.Name} › {attr.Name}", "other"));
            }

            foreach (var v in values)
                if (v.Option != null)
                    attrValueMap[(v.StudentId, v.ActivityAttributeId)] = v.Option.Name;
        }

        // --- Presentations ---
        // presRoleMap[(studentId, taskId)] = "P" (Prezentujúci) or "N" (Náhradník)
        List<TaskItem> presentations = new();
        Dictionary<(int studentId, int taskId), string> presRoleMap = new();
        if (request.IncludePresentations)
        {
            presentations = await _db.TaskItems
                .Include(t => t.Activity)
                .Include(t => t.PresentationStudents)
                .Where(t => t.IsPresentation
                         && t.Activity.GroupId == request.GroupId
                         && !t.Activity.IsArchived)
                .OrderBy(t => t.Activity.Name).ThenBy(t => t.Title)
                .ToListAsync();

            foreach (var p in presentations)
            {
                var dateStr = p.PresentationDate.HasValue
                    ? p.PresentationDate.Value.ToString("dd.MM.yyyy")
                    : "bez dátumu";
                headers.Add(($"{p.Activity.Name} › {p.Title} ({dateStr})", "presentations"));

                foreach (var ps in p.PresentationStudents)
                {
                    var label = ps.Role == PresentationRole.Presentee ? "✓ (Prezentujúci)" : "✓ (Náhradník)";
                    presRoleMap[(ps.StudentId, p.Id)] = label;
                }
            }
        }

        // ── 3. Build data rows ─────────────────────────────────────────────
        var rows = new List<List<string>>();
        foreach (var student in students)
        {
            var row = new List<string>();

            if (request.IncludeStudents)
            {
                if (request.IncludeStudentFirstName)  row.Add(student.FirstName);
                if (request.IncludeStudentLastName)   row.Add(student.LastName);
                if (request.IncludeStudentCardNumber) row.Add(student.CardNumber ?? "");
                if (request.IncludeStudentYear)       row.Add(student.Year?.ToString() ?? "");
                if (request.IncludeStudentEmail)      row.Add(student.Email ?? "");
            }

            if (request.IncludeAttendance)
            {
                var studentAtt = attendanceDateMap.GetValueOrDefault(student.Id) ?? [];
                if (request.IncludeAttendanceDetails)
                    foreach (var date in attendanceDates)
                        row.Add(studentAtt.GetValueOrDefault(date, ""));
                if (request.IncludeAttendanceSummary)
                {
                    var present = attendanceDates.Count(d => studentAtt.GetValueOrDefault(d) == "P");
                    var absent  = attendanceDates.Count(d => studentAtt.GetValueOrDefault(d) == "N");
                    var excused = attendanceDates.Count(d => studentAtt.GetValueOrDefault(d) == "O");
                    var total   = present + absent + excused;
                    var pct     = total > 0 ? Math.Round((double)present / total * 100, 1) : 0.0;
                    row.Add(present.ToString());
                    row.Add(absent.ToString());
                    row.Add(excused.ToString());
                    row.Add(pct.ToString("F1") + "%");
                }
            }

            if (request.IncludeActivities)
            {
                foreach (var activity in activityList)
                    row.Add(activityAssignedSet.Contains((student.Id, activity.Id)) ? "✓" : "");
            }

            if (request.IncludeTasks)
            {
                decimal grandTotal = 0;
                bool hasGrandAny = false;

                var allActivityIds = tasks.Select(t => t.ActivityId)
                    .Union(taskPresentations.Select(t => t.ActivityId))
                    .Distinct()
                    .OrderBy(id => tasks.FirstOrDefault(t => t.ActivityId == id)?.Activity.Name
                                 ?? taskPresentations.First(t => t.ActivityId == id).Activity.Name)
                    .ToList();

                foreach (var actId in allActivityIds)
                {
                    decimal actSum = 0;
                    bool hasAny = false;

                    if (request.IncludeTasksDetails)
                    {
                        foreach (var task in tasks.Where(t => t.ActivityId == actId))
                        {
                            if (evalMap.TryGetValue((student.Id, task.Id), out var eval))
                            {
                                row.Add(eval.Score.ToString("F2"));
                                actSum += eval.Score;
                                hasAny = true;
                            }
                            else
                            {
                                row.Add("");
                            }
                        }
                        foreach (var pres in taskPresentations.Where(t => t.ActivityId == actId))
                        {
                            if (evalMap.TryGetValue((student.Id, pres.Id), out var eval))
                            {
                                row.Add(eval.Score.ToString("F2"));
                                actSum += eval.Score;
                                hasAny = true;
                            }
                            else
                            {
                                row.Add("");
                            }
                        }
                    }
                    else
                    {
                        // Summary only — still need to sum all scores
                        foreach (var task in tasks.Where(t => t.ActivityId == actId))
                            if (evalMap.TryGetValue((student.Id, task.Id), out var eval))
                            { actSum += eval.Score; hasAny = true; }
                        foreach (var pres in taskPresentations.Where(t => t.ActivityId == actId))
                            if (evalMap.TryGetValue((student.Id, pres.Id), out var eval))
                            { actSum += eval.Score; hasAny = true; }
                    }

                    if (request.IncludeTasksSummary)
                        row.Add(hasAny ? actSum.ToString("F2") : "");
                    grandTotal += actSum;
                    if (hasAny) hasGrandAny = true;
                }

                if (request.IncludeTasksSummary)
                    row.Add(hasGrandAny ? grandTotal.ToString("F2") : "");
            }

            if (request.IncludeOtherAttributes)
            {
                foreach (var (attrId, _) in attrList)
                    row.Add(attrValueMap.GetValueOrDefault((student.Id, attrId), ""));
            }

            if (request.IncludePresentations)
            {
                foreach (var pres in presentations)
                    row.Add(presRoleMap.GetValueOrDefault((student.Id, pres.Id), ""));
            }

            rows.Add(row);
        }

        // ── 4. Generate file ───────────────────────────────────────────────
        return request.Format == "csv"
            ? BuildCsv(headers.Select(h => h.Text).ToList(), rows)
            : BuildXlsx(headers, rows);
    }

    // ── CSV builder ────────────────────────────────────────────────────────
    private static byte[] BuildCsv(List<string> headers, List<List<string>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(Escape)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(Escape)));
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Escape(string v) =>
        v.Contains(',') || v.Contains('"') || v.Contains('\n')
            ? $"\"{v.Replace("\"", "\"\"")}\""
            : v;

    // ── XLSX builder ───────────────────────────────────────────────────────
    // Section colour bands
    private static readonly Dictionary<string, string> SectionColours = new()
    {
        { "students",      "#1e3a5f" },  // dark navy
        { "attendance",    "#1a5276" },  // dark teal-blue
        { "att-sum",       "#0e3a50" },  // darker teal — attendance summary
        { "activities",    "#1e8449" },  // dark green
        { "tasks",         "#6c3483" },  // dark purple
        { "tasks-pres",    "#7d3c00" },  // dark brown — presentation scores
        { "tasks-sum",     "#4a235a" },  // deeper purple — activity total
        { "tasks-total",   "#2d1436" },  // darkest purple — grand total
        { "presentations", "#935116" },  // dark amber
        { "other",         "#117a65" },  // dark teal
    };

    private static byte[] BuildXlsx(List<(string Text, string Section)> headers, List<List<string>> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Export");

        int totalRows = rows.Count;
        int totalCols = headers.Count;

        // ── Header row ────────────────────────────────────────────────────
        for (int c = 0; c < totalCols; c++)
        {
            var cell   = ws.Cell(1, c + 1);
            var colour = SectionColours.GetValueOrDefault(headers[c].Section, "#2c3e50");

            cell.Value = headers[c].Text;
            cell.Style.Font.Bold      = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Font.FontSize  = 10;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(colour);
            cell.Style.Alignment.WrapText   = true;
            cell.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Border around every header cell
            cell.Style.Border.TopBorder    = XLBorderStyleValues.Thin;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            cell.Style.Border.LeftBorder   = XLBorderStyleValues.Thin;
            cell.Style.Border.RightBorder  = XLBorderStyleValues.Thin;
            cell.Style.Border.TopBorderColor    = XLColor.FromHtml("#ffffff40");
            cell.Style.Border.BottomBorderColor = XLColor.White;
            cell.Style.Border.LeftBorderColor   = XLColor.FromHtml("#ffffff30");
            cell.Style.Border.RightBorderColor  = XLColor.FromHtml("#ffffff30");
        }

        ws.Row(1).Height = 42;
        ws.SheetView.FreezeRows(1);

        // ── Data rows ─────────────────────────────────────────────────────
        var rowBgEven = XLColor.White;
        var rowBgOdd  = XLColor.FromHtml("#f5f7fa");
        var borderCol = XLColor.FromHtml("#d0d7de");

        for (int r = 0; r < totalRows; r++)
        {
            bool even = r % 2 == 0;
            var  bg   = even ? rowBgEven : rowBgOdd;

            for (int c = 0; c < rows[r].Count; c++)
            {
                var cell = ws.Cell(r + 2, c + 1);
                var val  = rows[r][c];

                // Numeric values → stored as numbers so Excel can sort/sum
                if (decimal.TryParse(val, out var num))
                    cell.Value = num;
                else
                    cell.Value = val;

                cell.Style.Fill.BackgroundColor = bg;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;

                // Thin border on every cell
                cell.Style.Border.TopBorder    = XLBorderStyleValues.Thin;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.LeftBorder   = XLBorderStyleValues.Thin;
                cell.Style.Border.RightBorder  = XLBorderStyleValues.Thin;
                cell.Style.Border.TopBorderColor    = borderCol;
                cell.Style.Border.BottomBorderColor = borderCol;
                cell.Style.Border.LeftBorderColor   = borderCol;
                cell.Style.Border.RightBorderColor  = borderCol;
            }
        }

        // Left-align text columns in the students section
        for (int c = 0; c < totalCols; c++)
        {
            if (headers[c].Section == "students")
                ws.Column(c + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        }

        // ── Section separator: thick left border at each section boundary ─
        string? lastSection = null;
        for (int c = 0; c < totalCols; c++)
        {
            if (lastSection != null && headers[c].Section != lastSection)
            {
                // Apply thick left border to the entire column (header + data)
                var rng = ws.Range(ws.Cell(1, c + 1), ws.Cell(totalRows + 1, c + 1));
                rng.Style.Border.LeftBorder      = XLBorderStyleValues.Medium;
                rng.Style.Border.LeftBorderColor = XLColor.FromHtml("#6c757d");
            }
            lastSection = headers[c].Section;
        }

        // Also add a thick right border on the very last column
        if (totalCols > 0)
        {
            var lastColRng = ws.Range(ws.Cell(1, totalCols), ws.Cell(totalRows + 1, totalCols));
            lastColRng.Style.Border.RightBorder      = XLBorderStyleValues.Medium;
            lastColRng.Style.Border.RightBorderColor = XLColor.FromHtml("#6c757d");
        }

        // ── Auto-fit columns, cap width ───────────────────────────────────
        ws.Columns().AdjustToContents(1, totalRows + 1);
        foreach (var col in ws.ColumnsUsed())
        {
            if (col.Width > 40) col.Width = 40;
            if (col.Width < 9)  col.Width = 9;
        }

        // ── Colour legend ─────────────────────────────────────────────────
        // Collect only the sections that are actually present in this export
        var sectionsUsed = headers.Select(h => h.Section).Distinct().ToList();

        var legendLabels = new Dictionary<string, string>
        {
            { "students",      "Študenti — základné informácie o študentoch (meno, číslo karty, ročník, email)" },
            { "attendance",    "Dochádzka — P = Prítomný  |  N = Neprítomný  |  O = Ospravedlnený" },
            { "activities",    "Aktivity — ✓ znamená, že študent je priradený k danej aktivite" },
            { "tasks",         "Úlohy a hodnotenia — číselné skóre za každú úlohu, prázdne ak nie je hodnotené" },
            { "tasks-pres",    "Prezentácie (skóre) — číselné skóre za prezentáciu, prázdne ak nie je hodnotené" },
            { "tasks-sum",     "Celkom za aktivitu — súčet skóre úloh a prezentácií v rámci aktivity" },
            { "tasks-total",   "Celkom (všetky aktivity) — súčet všetkých skóre za všetky aktivity" },
            { "presentations", "Prezentácie — ✓ (Prezentujúci) = prezentujúci študent  |  ✓ (Náhradník) = náhradník  |  prázdne = nepriradený" },
            { "other",         "Ostatné atribúty — vlastné hodnoty atribútov na študenta" },
        };

        int legendStartRow = totalRows + 3;   // one blank row gap after data

        // "Legend" title
        var titleCell = ws.Cell(legendStartRow, 1);
        titleCell.Value = "Legenda";
        titleCell.Style.Font.Bold     = true;
        titleCell.Style.Font.FontSize = 11;
        titleCell.Style.Font.FontColor = XLColor.FromHtml("#343a40");
        legendStartRow++;

        foreach (var section in sectionsUsed)
        {
            if (!legendLabels.TryGetValue(section, out var label)) continue;
            if (!SectionColours.TryGetValue(section, out var colour)) continue;

            // Colour swatch cell
            var swatchCell = ws.Cell(legendStartRow, 1);
            swatchCell.Value = "";
            swatchCell.Style.Fill.BackgroundColor = XLColor.FromHtml(colour);
            swatchCell.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
            swatchCell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#6c757d");
            ws.Column(1).Width = Math.Max(ws.Column(1).Width, 4);

            // Label cell (spans a few columns for readability)
            var labelCell = ws.Cell(legendStartRow, 2);
            labelCell.Value = label;
            labelCell.Style.Font.FontSize  = 10;
            labelCell.Style.Font.FontColor = XLColor.FromHtml("#343a40");
            labelCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Row(legendStartRow).Height = 16;
            legendStartRow++;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
