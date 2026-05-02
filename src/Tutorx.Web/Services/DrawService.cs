using Microsoft.EntityFrameworkCore;
using Tutorx.Web.Data;
using Tutorx.Web.Models.DTOs;

namespace Tutorx.Web.Services;

public class DrawService : IDrawService
{
    private readonly AppDbContext _db;

    public DrawService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetNextDrawBatchIdAsync(int groupId)
    {
        var max = await _db.DrawHistories
            .Where(d => d.GroupId == groupId)
            .MaxAsync(d => (int?)d.DrawBatchId) ?? 0;
        return max + 1;
    }

    public async Task<List<DrawHistoryDto>> GetHistoryAsync(int groupId, int limit = 50)
    {
        return await _db.DrawHistories
            .Where(d => d.GroupId == groupId && d.ActivityId == null)
            .OrderByDescending(d => d.DrawnAt)
            .Take(limit)
            .Include(d => d.Student)
            .Select(d => new DrawHistoryDto(d.Student.FirstName + " " + d.Student.LastName, d.DrawnAt, d.CycleNumber))
            .ToListAsync();
    }

    public async Task<List<DrawBatchDto>> GetBatchHistoryAsync(int groupId)
    {
        var records = await _db.DrawHistories
            .Where(d => d.GroupId == groupId)
            .OrderByDescending(d => d.DrawnAt)
            .Include(d => d.Student)
            .Include(d => d.Activity)
            .Include(d => d.TaskItem)
            .ToListAsync();

        // Group by DrawBatchId — every draw call assigns a unique batch id so each call
        // produces its own row; all students drawn in one call share the same batch id.
        var batches = records
            .GroupBy(d => d.DrawBatchId)
            .OrderByDescending(g => g.Max(d => d.DrawnAt.Ticks))
            .Select(g => new DrawBatchDto(
                g.First().Activity?.Name,
                g.First().TaskItem?.Title,
                g.OrderBy(d => d.Student.LastName)
                 .ThenBy(d => d.Student.FirstName)
                 .Select(d => new DrawStudentDto(
                     d.Student.FullName,
                     d.Role.HasValue ? (byte)d.Role.Value : null))
                 .ToList(),
                new DateTime(g.Max(d => d.DrawnAt.Ticks), DateTimeKind.Utc)
            ))
            .ToList();

        return batches;
    }
}
