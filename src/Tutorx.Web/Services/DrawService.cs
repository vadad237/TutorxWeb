using Microsoft.EntityFrameworkCore;
using Tutorx.Web.Data;
using Tutorx.Web.Models.DTOs;
using Tutorx.Web.Models.Entities;

namespace Tutorx.Web.Services;

public class DrawService : IDrawService
{
    private readonly AppDbContext _db;

    public DrawService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DrawResultDto> DrawNextAsync(int groupId)
    {
        var allActive = await _db.Students
            .Where(s => s.GroupId == groupId && s.IsActive)
            .Select(s => s.Id).ToListAsync();

        if (allActive.Count == 0)
            throw new InvalidOperationException("Žiadni aktívni študenti.");

        var currentCycle = await _db.DrawHistories
            .Where(d => d.GroupId == groupId)
            .MaxAsync(d => (int?)d.CycleNumber) ?? 0;

        var drawnThisCycle = await _db.DrawHistories
            .Where(d => d.GroupId == groupId && d.CycleNumber == currentCycle)
            .Select(d => d.StudentId).ToListAsync();

        var remaining = allActive.Except(drawnThisCycle).ToList();

        if (remaining.Count == 0)
        {
            currentCycle++;
            remaining = allActive;
        }

        var pickedId = remaining[Random.Shared.Next(remaining.Count)];
        var batchId = await GetNextDrawBatchIdAsync(groupId);

        _db.DrawHistories.Add(new DrawHistory
        {
            StudentId = pickedId,
            GroupId = groupId,
            CycleNumber = currentCycle,
            DrawBatchId = batchId
        });
        await _db.SaveChangesAsync();

        var student = await _db.Students.FindAsync(pickedId);
        return new DrawResultDto(pickedId, student!.FullName, currentCycle, remaining.Count - 1);
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
                     d.Student.FirstName + " " + d.Student.LastName,
                     d.Role.HasValue ? (byte)d.Role.Value : null))
                 .ToList(),
                new DateTime(g.Max(d => d.DrawnAt.Ticks), DateTimeKind.Utc)
            ))
            .ToList();

        return batches;
    }

    public async Task<BagStatusDto> GetBagStatusAsync(int groupId)
    {
        var totalActive = await _db.Students
            .CountAsync(s => s.GroupId == groupId && s.IsActive);

        var currentCycle = await _db.DrawHistories
            .Where(d => d.GroupId == groupId)
            .MaxAsync(d => (int?)d.CycleNumber) ?? 0;

        var drawnThisCycle = await _db.DrawHistories
            .CountAsync(d => d.GroupId == groupId && d.CycleNumber == currentCycle);

        // If no draws yet, cycle is 0 and remaining = total
        if (currentCycle == 0 && drawnThisCycle == 0)
        {
            return new BagStatusDto(totalActive, totalActive, 1);
        }

        var remaining = totalActive - drawnThisCycle;
        if (remaining <= 0)
        {
            return new BagStatusDto(totalActive, totalActive, currentCycle + 1);
        }

        return new BagStatusDto(remaining, totalActive, currentCycle);
    }

    public async Task ResetBagAsync(int groupId)
    {
        var currentCycle = await _db.DrawHistories
            .Where(d => d.GroupId == groupId)
            .MaxAsync(d => (int?)d.CycleNumber) ?? 0;

        var historyToRemove = _db.DrawHistories
            .Where(d => d.GroupId == groupId && d.CycleNumber == currentCycle);

        _db.DrawHistories.RemoveRange(historyToRemove);
        await _db.SaveChangesAsync();
    }
}
