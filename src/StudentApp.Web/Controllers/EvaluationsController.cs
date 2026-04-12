using Microsoft.AspNetCore.Mvc;
using StudentApp.Web.Models.ViewModels;
using StudentApp.Web.Services;

namespace StudentApp.Web.Controllers;

public class EvaluationsController : Controller
{
    private readonly IGroupService _groupService;
    private readonly IEvaluationService _evaluationService;

    public EvaluationsController(IGroupService groupService, IEvaluationService evaluationService)
    {
        _groupService = groupService;
        _evaluationService = evaluationService;
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

    [HttpGet]
    public async Task<IActionResult> Index(int? groupId)
    {
        await PopulateActiveGroupAsync();
        var gid = groupId ?? HttpContext.Session.GetActiveGroup();
        if (!gid.HasValue)
        {
            ViewBag.NoGroupSelected = true;
            return View((EvaluationIndexVm?)null);
        }

        var vm = await _evaluationService.GetEvaluationIndexAsync(gid.Value);
        if (vm == null) return NotFound();

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int studentId, int taskItemId)
    {
        await PopulateActiveGroupAsync();

        var existingId = await _evaluationService.GetExistingEvaluationIdAsync(studentId, taskItemId);
        if (existingId.HasValue)
            return RedirectToAction(nameof(Edit), new { id = existingId.Value });

        var info = await _evaluationService.GetStudentAndTaskInfoAsync(studentId, taskItemId);
        if (info == null) return NotFound();

        var vm = new EvaluationCreateVm
        {
            StudentId = studentId,
            TaskItemId = taskItemId,
            StudentName = info.Value.StudentName,
            TaskName = info.Value.TaskName
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EvaluationCreateVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        await _evaluationService.CreateEvaluationAsync(vm.StudentId, vm.TaskItemId, vm.Score, vm.Comment);

        TempData["Success"] = "Hodnotenie uložené.";
        var groupId = await _evaluationService.GetStudentGroupIdAsync(vm.StudentId);
        return RedirectToAction(nameof(Index), new { groupId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        await PopulateActiveGroupAsync();
        var vm = await _evaluationService.GetEvaluationForEditAsync(id);
        if (vm == null) return NotFound();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EvaluationEditVm vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateActiveGroupAsync();
            return View(vm);
        }

        var updated = await _evaluationService.UpdateEvaluationAsync(id, vm.Score, vm.Comment);
        if (!updated) return NotFound();

        TempData["Success"] = "Hodnotenie aktualizované.";
        var groupId = await _evaluationService.GetStudentGroupIdAsync(vm.StudentId);
        return RedirectToAction(nameof(Index), new { groupId });
    }
}
