using Microsoft.AspNetCore.Mvc;
using StudentApp.Web.Models.Entities;
using StudentApp.Web.Services;

namespace StudentApp.Web.Controllers;

public class TasksController : Controller
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(string title, int activityId, DateTime? presentationDate, bool isPresentation = false, decimal? maxScore = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Json(new { success = false, message = "Title is required." });

        var task = await _taskService.CreateTaskAsync(title, activityId, presentationDate, isPresentation, maxScore);
        return Json(new { success = true, taskId = task.Id, title = task.Title });
    }

    [HttpPost]
    public async Task<IActionResult> CreateNumbered(int activityId, int count)
    {
        if (count <= 0 || count > 100)
            return Json(new { success = false, message = "Počet musí byť medzi 1 a 100." });

        var tasks = await _taskService.CreateNumberedTasksAsync(activityId, count);
        var result = tasks.Select(t => new { taskId = t.Id, number = int.Parse(t.Title) }).ToList();
        return Json(new { success = true, tasks = result });
    }

    [HttpPost]
    public async Task<IActionResult> SetTitle(int id, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Json(new { success = false, message = "Title required." });

        var (success, message) = await _taskService.SetTitleAsync(id, title);
        if (!success) return Json(new { success = false, message });
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> SetDate(int id, DateTime? presentationDate)
    {
        var (success, message) = await _taskService.SetDateAsync(id, presentationDate);
        if (!success) return Json(new { success = false, message });
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> SetMaxScore(int id, decimal? maxScore)
    {
        var (success, message) = await _taskService.SetMaxScoreAsync(id, maxScore);
        if (!success) return Json(new { success = false, message });
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> SetPresentationStudents(int taskId, int[]? studentIds)
    {
        await _taskService.SetPresentationStudentsAsync(taskId, studentIds);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> SetPresentationStudentsByRole(int taskId, int[]? studentIds, int role = 0)
    {
        await _taskService.SetPresentationStudentsByRoleAsync(taskId, studentIds, (PresentationRole)role);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var (found, activityId) = await _taskService.DeleteTaskAsync(id);
        if (!found)
            return Json(new { success = false, message = "Položka nebola nájdená." });

        return Json(new { success = true, activityId });
    }

    [HttpPost]
    public async Task<IActionResult> BulkDelete([FromBody] int[] ids)
    {
        if (ids == null || ids.Length == 0)
            return Json(new { success = false, message = "Žiadne položky neboli vybrané." });

        await _taskService.BulkDeleteTasksAsync(ids);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> AutoAssignNumbered([FromBody] AutoAssignNumberedRequest req)
    {
        if (req.TaskIds == null || req.TaskIds.Length == 0)
            return Json(new { success = false, message = "Žiadne zadania neboli vybrané." });

        var (success, message) = await _taskService.AutoAssignNumberedTasksAsync(req.ActivityId, req.TaskIds);
        return Json(new { success, message });
    }

    [HttpGet]
    public async Task<IActionResult> GetEligiblePresentationStudents(int taskId, bool includeAlreadyAssigned = false, int? role = null)
    {
        PresentationRole? parsedRole = role.HasValue ? (PresentationRole)role.Value : null;
        var eligible = await _taskService.GetEligiblePresentationStudentsAsync(taskId, includeAlreadyAssigned, parsedRole);
        if (eligible == null) return NotFound();

        return Json(eligible);
    }
}

public record AutoAssignNumberedRequest(int ActivityId, int[] TaskIds);
