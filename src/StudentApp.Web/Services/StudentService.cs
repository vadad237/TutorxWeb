using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.DTOs;
using StudentApp.Web.Models.Entities;
using StudentApp.Web.Models.ViewModels;

namespace StudentApp.Web.Services;

public class StudentService : IStudentService
{
    private readonly AppDbContext _db;

    public StudentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<StudentSummaryVm>> GetStudentsByGroupAsync(int groupId)
    {
        return await _db.Students
            .Where(s => s.GroupId == groupId)
            .Include(s => s.Attendances)
            .Include(s => s.Evaluations)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new StudentSummaryVm
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                FullName = s.FirstName + " " + s.LastName,
                Email = s.Email,
                CardNumber = s.CardNumber,
                GroupNumber = s.GroupNumber,
                Year = s.Year,
                IsActive = s.IsActive,
                AbsenceCount = s.Attendances.Count(a => a.Status == AttendanceStatus.Absent),
                TotalScore = s.Evaluations.Any() ? s.Evaluations.Sum(e => e.Score) : null,
                GroupId = s.GroupId
            })
            .ToListAsync();
    }

    public async Task<bool> IsCardNumberTakenAsync(string cardNumber, int? excludeStudentId = null)
    {
        var trimmed = cardNumber.Trim();
        return await _db.Students.AnyAsync(s =>
            s.CardNumber == trimmed &&
            (excludeStudentId == null || s.Id != excludeStudentId));
    }

    public async Task<Student> CreateStudentAsync(string firstName, string lastName, string? email, string? cardNumber, int? year, string? groupNumber, int groupId)
    {
        var student = new Student
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email?.Trim(),
            CardNumber = cardNumber?.Trim(),
            Year = year,
            GroupNumber = groupNumber?.Trim(),
            GroupId = groupId
        };
        _db.Students.Add(student);
        await _db.SaveChangesAsync();
        return student;
    }

    public async Task<Student?> GetStudentWithGroupAsync(int id)
    {
        return await _db.Students.Include(s => s.Group).FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Student?> UpdateStudentAsync(int id, string firstName, string lastName, string? email, string? cardNumber, int? year, string? groupNumber, bool isActive)
    {
        var student = await _db.Students.FindAsync(id);
        if (student == null) return null;

        student.FirstName = firstName.Trim();
        student.LastName = lastName.Trim();
        student.Email = email?.Trim();
        student.CardNumber = cardNumber?.Trim();
        student.Year = year;
        student.GroupNumber = groupNumber?.Trim();
        student.IsActive = isActive;
        await _db.SaveChangesAsync();
        return student;
    }

    public async Task<StudentDetailsVm?> GetStudentDetailsAsync(int id)
    {
        var student = await _db.Students
            .Include(s => s.Group)
            .Include(s => s.DrawHistories)
            .Include(s => s.Attendances)
            .Include(s => s.Evaluations).ThenInclude(e => e.TaskItem).ThenInclude(t => t.Activity)
            .Include(s => s.Assignments).ThenInclude(a => a.Activity)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null) return null;

        return new StudentDetailsVm
        {
            Id = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            FullName = student.FullName,
            Email = student.Email,
            CardNumber = student.CardNumber,
            Year = student.Year,
            GroupNumber = student.GroupNumber,
            IsActive = student.IsActive,
            GroupId = student.GroupId,
            GroupName = student.Group.Name,
            CreatedAt = student.CreatedAt,
            DrawHistory = student.DrawHistories
                .OrderByDescending(d => d.DrawnAt)
                .Select(d => new DrawHistoryDto(student.FullName, d.DrawnAt, d.CycleNumber))
                .ToList(),
            AttendanceSummary = new AttendanceSummaryItemVm
            {
                StudentId = student.Id,
                FullName = student.FullName,
                PresentCount = student.Attendances.Count(a => a.Status == AttendanceStatus.Present),
                AbsentCount = student.Attendances.Count(a => a.Status == AttendanceStatus.Absent),
                ExcusedCount = student.Attendances.Count(a => a.Status == AttendanceStatus.Excused),
                TotalCount = student.Attendances.Count
            },
            Evaluations = student.Evaluations
                .OrderBy(e => e.TaskItem.Activity.Name).ThenBy(e => e.TaskItem.Title)
                .Select(e => new EvaluationItemVm
                {
                    Id = e.Id,
                    ActivityId = e.TaskItem.ActivityId,
                    ActivityName = e.TaskItem.Activity.Name,
                    TaskName = e.TaskItem.Title,
                    Score = e.Score,
                    Comment = e.Comment,
                    EvaluatedAt = e.EvaluatedAt
                }).ToList(),
            AssignedActivities = student.Assignments
                .Where(a => !a.Activity.IsArchived)
                .OrderBy(a => a.Activity.Name)
                .Select(a => new AssignedActivityVm(a.ActivityId, a.Activity.Name))
                .ToList()
        };
    }

    public async Task<bool> DeleteStudentAsync(int id)
    {
        var student = await _db.Students.FindAsync(id);
        if (student == null) return false;

        await _db.StudentAttributeValues.Where(v => v.StudentId == id).ExecuteDeleteAsync();
        await _db.PresentationStudents.Where(ps => ps.StudentId == id).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => e.StudentId == id).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => a.StudentId == id).ExecuteDeleteAsync();
        await _db.DrawHistories.Where(d => d.StudentId == id).ExecuteDeleteAsync();
        await _db.Attendances.Where(a => a.StudentId == id).ExecuteDeleteAsync();

        _db.Students.Remove(student);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task BulkDeleteStudentsAsync(List<int> ids)
    {
        await _db.StudentAttributeValues.Where(v => ids.Contains(v.StudentId)).ExecuteDeleteAsync();
        await _db.PresentationStudents.Where(ps => ids.Contains(ps.StudentId)).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => ids.Contains(e.StudentId)).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => ids.Contains(a.StudentId)).ExecuteDeleteAsync();
        await _db.DrawHistories.Where(d => ids.Contains(d.StudentId)).ExecuteDeleteAsync();
        await _db.Attendances.Where(a => ids.Contains(a.StudentId)).ExecuteDeleteAsync();
        await _db.Students.Where(s => ids.Contains(s.Id)).ExecuteDeleteAsync();
    }

    public async Task<Student?> ToggleActiveAsync(int id)
    {
        var student = await _db.Students.FindAsync(id);
        if (student == null) return null;

        student.IsActive = !student.IsActive;
        await _db.SaveChangesAsync();
        return student;
    }

    public async Task BulkSetActiveAsync(List<int> ids, bool active)
    {
        await _db.Students.Where(s => ids.Contains(s.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, active));
    }

    public async Task<List<StudentSummaryVm>> GetActiveStudentsByGroupAsync(int groupId)
    {
        return await _db.Students
            .Where(s => s.GroupId == groupId && s.IsActive)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new StudentSummaryVm
            {
                Id = s.Id,
                FullName = s.FirstName + " " + s.LastName,
                GroupId = s.GroupId
            })
            .ToListAsync();
    }
}
