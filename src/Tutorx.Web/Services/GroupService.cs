using Microsoft.EntityFrameworkCore;
using Tutorx.Web.Data;
using Tutorx.Web.Models.Entities;
using Tutorx.Web.Models.ViewModels;

namespace Tutorx.Web.Services;

public class GroupService : IGroupService
{
    private readonly AppDbContext _db;

    public GroupService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<GroupListItem>> GetNonArchivedGroupsAsync()
    {
        return await _db.Groups.Where(g => !g.IsArchived)
            .Select(g => new GroupListItem(g.Id, g.Name))
            .ToListAsync();
    }

    public async Task<List<GroupSummaryVm>> GetGroupSummariesAsync(bool showArchived)
    {
        var query = _db.Groups.AsQueryable();
        if (!showArchived)
            query = query.Where(g => !g.IsArchived);

        return await query
            .Include(g => g.Students)
            .Select(g => new GroupSummaryVm
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                StudentCount = g.Students.Count,
                ActiveStudentCount = g.Students.Count(s => s.IsActive),
                IsArchived = g.IsArchived,
                CreatedAt = g.CreatedAt
            })
            .OrderBy(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<Group?> GetGroupByIdAsync(int id)
    {
        return await _db.Groups.FindAsync(id);
    }

    public async Task<string?> GetGroupNameAsync(int groupId)
    {
        return await _db.Groups.Where(g => g.Id == groupId)
            .Select(g => g.Name).FirstOrDefaultAsync();
    }

    public async Task<bool> GroupNameExistsAsync(string name, int? excludeId = null)
    {
        var query = _db.Groups.Where(g => g.Name == name.Trim());
        if (excludeId.HasValue)
            query = query.Where(g => g.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<Group> CreateGroupAsync(string name, string? description)
    {
        var group = new Group
        {
            Name = name.Trim(),
            Description = description?.Trim()
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();
        return group;
    }

    public async Task<Group?> UpdateGroupAsync(int id, string name, string? description)
    {
        var group = await _db.Groups.FindAsync(id);
        if (group == null)
            return null;

        group.Name = name.Trim();
        group.Description = description?.Trim();
        await _db.SaveChangesAsync();
        return group;
    }

    public async Task<GroupDetailsVm?> GetGroupDetailsAsync(int id)
    {
        var group = await _db.Groups
            .Include(g => g.Students)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
            return null;

        return new GroupDetailsVm
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            TotalStudents = group.Students.Count,
            ActiveStudents = group.Students.Count(s => s.IsActive),
            InactiveStudents = group.Students.Count(s => !s.IsActive),
            CreatedAt = group.CreatedAt,
            Students = group.Students.Select(s => new StudentSummaryVm
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                IsActive = s.IsActive,
                GroupId = s.GroupId
            }).OrderBy(s => s.FullName).ToList()
        };
    }

    public async Task<bool> DeleteGroupAsync(int id)
    {
        var exists = await _db.Groups.AnyAsync(g => g.Id == id);
        if (!exists)
            return false;

        var studentIds = _db.Students.Where(s => s.GroupId == id).Select(s => s.Id);
        var activityIds = _db.Activities.Where(a => a.GroupId == id).Select(a => a.Id);
        var taskIds = _db.TaskItems.Where(t => activityIds.Contains(t.ActivityId)).Select(t => t.Id);
        var attrIds = _db.ActivityAttributes.Where(a => activityIds.Contains(a.ActivityId)).Select(a => a.Id);

        await _db.StudentAttributeValues.Where(v => studentIds.Contains(v.StudentId) || attrIds.Contains(v.ActivityAttributeId)).ExecuteDeleteAsync();
        await _db.PresentationStudents.Where(ps => studentIds.Contains(ps.StudentId) || taskIds.Contains(ps.TaskItemId)).ExecuteDeleteAsync();
        await _db.Evaluations.Where(e => studentIds.Contains(e.StudentId)).ExecuteDeleteAsync();
        await _db.Assignments.Where(a => studentIds.Contains(a.StudentId)).ExecuteDeleteAsync();
        await _db.DrawHistories.Where(d => studentIds.Contains(d.StudentId)).ExecuteDeleteAsync();
        await _db.Attendances.Where(a => studentIds.Contains(a.StudentId) || a.GroupId == id).ExecuteDeleteAsync();
        await _db.Students.Where(s => s.GroupId == id).ExecuteDeleteAsync();

        await _db.ActivityAttributeOptions.Where(o => attrIds.Contains(o.ActivityAttributeId)).ExecuteDeleteAsync();
        await _db.ActivityAttributes.Where(a => activityIds.Contains(a.ActivityId)).ExecuteDeleteAsync();
        await _db.TaskItems.Where(t => activityIds.Contains(t.ActivityId)).ExecuteDeleteAsync();
        await _db.Activities.Where(a => a.GroupId == id).ExecuteDeleteAsync();

        await _db.DrawHistories.Where(d => d.GroupId == id).ExecuteDeleteAsync();
        await _db.Groups.Where(g => g.Id == id).ExecuteDeleteAsync();

        return true;
    }

    public async Task<(bool Success, string Message)> ArchiveGroupAsync(int id)
    {
        var group = await _db.Groups
            .Include(g => g.Activities).ThenInclude(a => a.Tasks)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
            return (false, "Skupina nebola nájdená.");

        group.IsArchived = true;
        await _db.SaveChangesAsync();

        return (true, $"Skupina '{group.Name}' bola archivovaná.");
    }

    public async Task<bool> UnarchiveGroupAsync(int id)
    {
        var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == id);
        if (group == null)
            return false;

        group.IsArchived = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> GroupExistsAsync(int id)
    {
        return await _db.Groups.AnyAsync(g => g.Id == id);
    }
}
