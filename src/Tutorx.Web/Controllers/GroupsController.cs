using Microsoft.AspNetCore.Mvc;
using Tutorx.Web.Models.ViewModels;
using Tutorx.Web.Services;

namespace Tutorx.Web.Controllers;

public class GroupsController : Controller
{
    private readonly IGroupService _groupService;

    public GroupsController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    private async Task PopulateActiveGroupAsync()
    {
        var groups = await _groupService.GetNonArchivedGroupsAsync();
        ViewBag.AllGroups = groups.Select(g => new { g.Id, g.Name }).ToList();
        var activeId = HttpContext.Session.GetActiveGroup();
        if (activeId.HasValue)
        {
            var name = groups.FirstOrDefault(g => g.Id == activeId.Value)?.Name;
            ViewData["ActiveGroupName"] = name;
        }
    }

    public async Task<IActionResult> Index(bool showArchived = false)
    {
        await PopulateActiveGroupAsync();
        var groups = await _groupService.GetGroupSummariesAsync(showArchived);

        ViewBag.ShowArchived = showArchived;
        return View(groups);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateActiveGroupAsync();
        return View(new GroupCreateVm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(GroupCreateVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        if (await _groupService.GroupNameExistsAsync(vm.Name))
        {
            ModelState.AddModelError("Name", "A group with this name already exists.");
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        var group = await _groupService.CreateGroupAsync(vm.Name, vm.Description);

        TempData["Success"] = $"Skupina '{group.Name}' bola úspešne vytvorená.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        await PopulateActiveGroupAsync();
        var group = await _groupService.GetGroupByIdAsync(id);
        if (group == null) return NotFound();

        return View(new GroupEditVm
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, GroupEditVm vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        if (await _groupService.GroupNameExistsAsync(vm.Name, id))
        {
            ModelState.AddModelError("Name", "A group with this name already exists.");
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        var group = await _groupService.UpdateGroupAsync(id, vm.Name, vm.Description);
        if (group == null) return NotFound();

        TempData["Success"] = $"Skupina '{group.Name}' bola úspešne aktualizovaná.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        await PopulateActiveGroupAsync();
        var vm = await _groupService.GetGroupDetailsAsync(id);
        if (vm == null) return NotFound();

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _groupService.DeleteGroupAsync(id);
        if (!deleted)
            return Json(new { success = false, message = "Group not found." });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Archive(int id)
    {
        var (success, message) = await _groupService.ArchiveGroupAsync(id);
        return Json(new { success, message });
    }

    [HttpPost]
    public async Task<IActionResult> Unarchive(int id)
    {
        var success = await _groupService.UnarchiveGroupAsync(id);
        if (!success)
            return Json(new { success = false, message = "Group not found." });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id)
    {
        var exists = await _groupService.GroupExistsAsync(id);
        if (!exists) return NotFound();

        HttpContext.Session.SetActiveGroup(id);
        return Redirect(Request.Headers.Referer.ToString() ?? Url.Action("Index")!);
    }
}
