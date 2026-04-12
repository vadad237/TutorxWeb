using Microsoft.AspNetCore.Mvc;
using StudentApp.Web.Services;

namespace StudentApp.Web.Controllers;

[IgnoreAntiforgeryToken]
public class ActivityAttributesController : Controller
{
    private readonly IActivityAttributeService _attributeService;

    public ActivityAttributesController(IActivityAttributeService attributeService) => _attributeService = attributeService;

    // ── Attribute CRUD ────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create(int activityId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Name is required." });

        var attr = await _attributeService.CreateAttributeAsync(activityId, name);
        return Json(new { success = true, id = attr.Id, name = attr.Name });
    }

    [HttpPost]
    public async Task<IActionResult> Rename(int id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Name is required." });

        var found = await _attributeService.RenameAttributeAsync(id, name);
        if (!found) return NotFound();

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var found = await _attributeService.DeleteAttributeAsync(id);
        if (!found) return NotFound();

        return Json(new { success = true });
    }

    // ── Option CRUD ───────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> AddOption(int attributeId, string name, string color = "secondary")
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Name is required." });

        var option = await _attributeService.AddOptionAsync(attributeId, name, color);
        return Json(new { success = true, id = option.Id, name = option.Name, color = option.Color });
    }

    [HttpPost]
    public async Task<IActionResult> EditOption(int id, string name, string color = "secondary")
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Name is required." });

        var found = await _attributeService.EditOptionAsync(id, name, color);
        if (!found) return NotFound();

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteOption(int id)
    {
        var found = await _attributeService.DeleteOptionAsync(id);
        if (!found) return NotFound();

        return Json(new { success = true });
    }

    // ── Student value ─────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> SetValue(int studentId, int attributeId, int? optionId)
    {
        await _attributeService.SetValueAsync(studentId, attributeId, optionId);
        return Json(new { success = true });
    }
}
