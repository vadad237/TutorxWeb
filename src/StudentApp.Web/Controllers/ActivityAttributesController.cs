using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentApp.Web.Data;
using StudentApp.Web.Models.Entities;

namespace StudentApp.Web.Controllers;

[IgnoreAntiforgeryToken]
public class ActivityAttributesController : Controller
{
    private readonly AppDbContext _db;

    public ActivityAttributesController(AppDbContext db) => _db = db;

    // ── Attribute CRUD ────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create(int activityId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Name is required." });

        var attr = new ActivityAttribute { ActivityId = activityId, Name = name.Trim() };
        _db.ActivityAttributes.Add(attr);
        await _db.SaveChangesAsync();
        return Json(new { success = true, id = attr.Id, name = attr.Name });
    }

    [HttpPost]
    public async Task<IActionResult> Rename(int id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Name is required." });

        var attr = await _db.ActivityAttributes.FindAsync(id);
        if (attr == null) return NotFound();

        attr.Name = name.Trim();
        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var attr = await _db.ActivityAttributes.FindAsync(id);
        if (attr == null) return NotFound();

        // Remove student values first (FK is Restrict to avoid multi-cascade-path)
        var values = _db.StudentAttributeValues.Where(v => v.ActivityAttributeId == id);
        _db.StudentAttributeValues.RemoveRange(values);

        _db.ActivityAttributes.Remove(attr);
        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    // ── Option CRUD ───────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> AddOption(int attributeId, string name, string color = "secondary")
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Name is required." });

        var option = new ActivityAttributeOption
        {
            ActivityAttributeId = attributeId,
            Name = name.Trim(),
            Color = color
        };
        _db.ActivityAttributeOptions.Add(option);
        await _db.SaveChangesAsync();
        return Json(new { success = true, id = option.Id, name = option.Name, color = option.Color });
    }

    [HttpPost]
    public async Task<IActionResult> EditOption(int id, string name, string color = "secondary")
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Name is required." });

        var option = await _db.ActivityAttributeOptions.FindAsync(id);
        if (option == null) return NotFound();

        option.Name = name.Trim();
        option.Color = color;
        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteOption(int id)
    {
        var option = await _db.ActivityAttributeOptions.FindAsync(id);
        if (option == null) return NotFound();

        _db.ActivityAttributeOptions.Remove(option);
        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    // ── Student value ─────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> SetValue(int studentId, int attributeId, int? optionId)
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
        return Json(new { success = true });
    }
}
