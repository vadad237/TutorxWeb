using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;

namespace StudentApp.Web.Services;

public class ActivityAttributeService : IActivityAttributeService
{
    private readonly AppDbContext _db;

    public ActivityAttributeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ActivityAttribute> CreateAttributeAsync(int activityId, string name)
    {
        var attr = new ActivityAttribute { ActivityId = activityId, Name = name.Trim() };
        _db.ActivityAttributes.Add(attr);
        await _db.SaveChangesAsync();
        return attr;
    }

    public async Task<bool> RenameAttributeAsync(int id, string name)
    {
        var attr = await _db.ActivityAttributes.FindAsync(id);
        if (attr == null) return false;

        attr.Name = name.Trim();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAttributeAsync(int id)
    {
        var attr = await _db.ActivityAttributes.FindAsync(id);
        if (attr == null) return false;

        var values = _db.StudentAttributeValues.Where(v => v.ActivityAttributeId == id);
        _db.StudentAttributeValues.RemoveRange(values);

        _db.ActivityAttributes.Remove(attr);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ActivityAttributeOption> AddOptionAsync(int attributeId, string name, string color)
    {
        var option = new ActivityAttributeOption
        {
            ActivityAttributeId = attributeId,
            Name = name.Trim(),
            Color = color
        };
        _db.ActivityAttributeOptions.Add(option);
        await _db.SaveChangesAsync();
        return option;
    }

    public async Task<bool> EditOptionAsync(int id, string name, string color)
    {
        var option = await _db.ActivityAttributeOptions.FindAsync(id);
        if (option == null) return false;

        option.Name = name.Trim();
        option.Color = color;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteOptionAsync(int id)
    {
        var option = await _db.ActivityAttributeOptions.FindAsync(id);
        if (option == null) return false;

        _db.ActivityAttributeOptions.Remove(option);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task SetValueAsync(int studentId, int attributeId, int? optionId)
    {
        var existing = await _db.StudentAttributeValues
            .FirstOrDefaultAsync(v => v.StudentId == studentId && v.ActivityAttributeId == attributeId);

        if (existing == null)
        {
            _db.StudentAttributeValues.Add(new StudentAttributeValue
            {
                StudentId = studentId,
                ActivityAttributeId = attributeId,
                OptionId = optionId
            });
        }
        else
        {
            existing.OptionId = optionId;
        }

        await _db.SaveChangesAsync();
    }
}
