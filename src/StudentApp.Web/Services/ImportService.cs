using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.DTOs;
using StudentApp.Web.Models.Entities;

namespace StudentApp.Web.Services;

public class ImportService : IImportService
{
    private readonly AppDbContext _db;

    public ImportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ImportPreviewDto> ParseFileAsync(Stream fileStream, string fileName, int groupId)
    {
        var group = await _db.Groups.FindAsync(groupId)
            ?? throw new InvalidOperationException("Group not found.");

        var existingStudents = await _db.Students
            .Where(s => s.GroupId == groupId)
            .Select(s => new { s.FirstName, s.LastName })
            .ToListAsync();

        var existingSet = new HashSet<string>(
            existingStudents.Select(s => $"{s.FirstName.Trim().ToLowerInvariant()}|{s.LastName.Trim().ToLowerInvariant()}")
        );

        List<ImportRowDto> rows;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (ext == ".csv")
        {
            rows = ParseCsv(fileStream, existingSet);
        }
        else if (ext == ".xlsx")
        {
            rows = ParseXlsx(fileStream, existingSet);
        }
        else
        {
            throw new InvalidOperationException("Unsupported file format. Use CSV or XLSX.");
        }

        return new ImportPreviewDto(rows, groupId, group.Name);
    }

    private List<ImportRowDto> ParseCsv(Stream stream, HashSet<string> existingSet)
    {
        var rows = new List<ImportRowDto>();

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = reader.ReadToEnd();

        // Detect separator
        var separator = content.Contains(';') ? ";" : ",";

        using var stringReader = new StringReader(content);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = separator,
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(stringReader, config);
        csv.Read();
        csv.ReadHeader();

        var headers = csv.HeaderRecord?.Select(h => h.Trim().ToLowerInvariant()).ToArray() ?? [];
        var firstNameIdx  = Array.FindIndex(headers, h => FirstNameAliases.Contains(h));
        var lastNameIdx   = Array.FindIndex(headers, h => LastNameAliases.Contains(h));
        var emailIdx      = Array.FindIndex(headers, h => EmailAliases.Contains(h));
        var cardNumberIdx = Array.FindIndex(headers, h => CardNumberAliases.Contains(h));
        var yearIdx       = Array.FindIndex(headers, h => YearAliases.Contains(h));

        if (firstNameIdx == -1 || lastNameIdx == -1)
        {
            rows.Add(new ImportRowDto("", "", null, null, null, ImportRowStatus.Error, "Missing required columns: Name/FirstName/Meno and Surname/LastName/Priezvisko"));
            return rows;
        }

        while (csv.Read())
        {
            var firstName = csv.GetField(firstNameIdx)?.Trim() ?? "";
            var lastName = csv.GetField(lastNameIdx)?.Trim() ?? "";
            var email = emailIdx >= 0 ? csv.GetField(emailIdx)?.Trim() : null;
            var cardNumber = cardNumberIdx >= 0 ? csv.GetField(cardNumberIdx)?.Trim() : null;
            int? year = null;
            if (yearIdx >= 0 && int.TryParse(csv.GetField(yearIdx)?.Trim(), out var parsedYear))
                year = parsedYear;

            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
                continue; // Skip empty rows

            if (string.IsNullOrWhiteSpace(firstName))
            {
                rows.Add(new ImportRowDto(firstName, lastName, email, cardNumber, year, ImportRowStatus.Error, "Missing FirstName"));
                continue;
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                rows.Add(new ImportRowDto(firstName, lastName, email, cardNumber, year, ImportRowStatus.Error, "Missing LastName"));
                continue;
            }

            var key = $"{firstName.ToLowerInvariant()}|{lastName.ToLowerInvariant()}";
            if (existingSet.Contains(key))
            {
                rows.Add(new ImportRowDto(firstName, lastName, email, cardNumber, year, ImportRowStatus.Duplicate, "Student already exists in group"));
            }
            else
            {
                rows.Add(new ImportRowDto(firstName, lastName, email, cardNumber, year, ImportRowStatus.Valid, null));
            }
        }

        return rows;
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    /// <summary>Returns the column number (1-based) for the first matching header alias, or -1 if none found.</summary>
    private static int GetCol(Dictionary<string, int> headers, string[] aliases)
    {
        foreach (var alias in aliases)
            if (headers.TryGetValue(alias, out var col)) return col;
        return -1;
    }

    /// <summary>Reads a cell as string, handling both text and numeric cells correctly.</summary>
    private static string GetCellString(IXLCell cell)
    {
        if (cell.DataType == XLDataType.Number)
            return ((long)cell.GetDouble()).ToString();
        return cell.GetString().Trim();
    }

    // All recognised aliases for each field (lowercase, no diacritics handled separately)
    private static readonly string[] FirstNameAliases   = ["firstname", "name", "meno"];
    private static readonly string[] LastNameAliases    = ["lastname", "surname", "priezvisko"];
    private static readonly string[] EmailAliases       = ["email", "e-mail", "e mail"];
    private static readonly string[] CardNumberAliases  = ["cardnumber", "card", "karta", "card number", "cardno", "číslo karty", "cislo karty", "číslokarty"];
    private static readonly string[] YearAliases        = ["year", "rocnik", "ročník", "rocník", "yr", "ročnik"];

    private static bool IsKnownHeader(string h) =>
        FirstNameAliases.Contains(h) || LastNameAliases.Contains(h) ||
        EmailAliases.Contains(h)     || CardNumberAliases.Contains(h) ||
        YearAliases.Contains(h);

    private List<ImportRowDto> ParseXlsx(Stream stream, HashSet<string> existingSet)
    {
        var rows = new List<ImportRowDto>();

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        // Find the actual header row — skip metadata rows at the top
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        int headerRowNum = -1;
        var headers = new Dictionary<string, int>();

        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (int r = 1; r <= Math.Min(lastRowUsed, 20); r++)
        {
            var candidate = new Dictionary<string, int>();
            for (int col = 1; col <= lastCol; col++)
            {
                var val = worksheet.Cell(r, col).GetString().Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(val))
                    candidate[val] = col;
            }
            if (IsKnownHeader(candidate.Keys.FirstOrDefault() ?? "") ||
                candidate.Keys.Any(IsKnownHeader))
            {
                headerRowNum = r;
                headers = candidate;
                break;
            }
        }

        if (headerRowNum == -1)
        {
            rows.Add(new ImportRowDto("", "", null, null, null, ImportRowStatus.Error, "Could not find a header row with recognised column names (expected Meno/Name, Priezvisko/Surname, etc.)"));
            return rows;
        }

        int firstNameCol  = GetCol(headers, FirstNameAliases);
        int lastNameCol   = GetCol(headers, LastNameAliases);
        int emailCol      = GetCol(headers, EmailAliases);
        int cardNumberCol = GetCol(headers, CardNumberAliases);
        int yearCol       = GetCol(headers, YearAliases);

        if (firstNameCol == -1 || lastNameCol == -1)
        {
            rows.Add(new ImportRowDto("", "", null, null, null, ImportRowStatus.Error, "Missing required columns: Name/FirstName/Meno and Surname/LastName/Priezvisko"));
            return rows;
        }

        for (int row = headerRowNum + 1; row <= lastRowUsed; row++)
        {
            var firstName  = GetCellString(worksheet.Cell(row, firstNameCol));
            var lastName   = GetCellString(worksheet.Cell(row, lastNameCol));
            var email      = emailCol > 0 ? NullIfEmpty(GetCellString(worksheet.Cell(row, emailCol))) : null;
            var cardNumber = cardNumberCol > 0 ? NullIfEmpty(GetCellString(worksheet.Cell(row, cardNumberCol))) : null;
            int? year = null;
            if (yearCol > 0 && int.TryParse(GetCellString(worksheet.Cell(row, yearCol)), out var parsedYear))
                year = parsedYear;

            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
                continue;

            if (string.IsNullOrWhiteSpace(firstName))
            {
                rows.Add(new ImportRowDto(firstName, lastName, email, cardNumber, year, ImportRowStatus.Error, "Missing FirstName"));
                continue;
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                rows.Add(new ImportRowDto(firstName, lastName, email, cardNumber, year, ImportRowStatus.Error, "Missing LastName"));
                continue;
            }

            var key = $"{firstName.ToLowerInvariant()}|{lastName.ToLowerInvariant()}";
            if (existingSet.Contains(key))
            {
                rows.Add(new ImportRowDto(firstName, lastName, email, cardNumber, year, ImportRowStatus.Duplicate, "Student already exists in group"));
            }
            else
            {
                rows.Add(new ImportRowDto(firstName, lastName, email, cardNumber, year, ImportRowStatus.Valid, null));
            }
        }

        return rows;
    }

    public async Task<int> ImportStudentsAsync(int groupId, List<ImportRowDto> rows)
    {
        var count = 0;
        foreach (var row in rows)
        {
            _db.Students.Add(new Student
            {
                FirstName = row.FirstName.Trim(),
                LastName = row.LastName.Trim(),
                Email = string.IsNullOrWhiteSpace(row.Email) ? null : row.Email.Trim(),
                CardNumber = string.IsNullOrWhiteSpace(row.CardNumber) ? null : row.CardNumber.Trim(),
                Year = row.Year,
                GroupId = groupId,
                IsActive = true
            });
            count++;
        }
        await _db.SaveChangesAsync();
        return count;
    }
}
