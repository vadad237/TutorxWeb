using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> Create(string title, int activityId, DateTime? presentationDate, bool isPresentation = false)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Json(new { success = false, message = "Title is required." });

        var task = await _taskService.CreateTaskAsync(title, activityId, presentationDate, isPresentation);
        return Json(new { success = true, taskId = task.Id, title = task.Title });
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPresentationStudents(int taskId, int[]? studentIds)
    {
        await _taskService.SetPresentationStudentsAsync(taskId, studentIds);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var (found, activityId) = await _taskService.DeleteTaskAsync(id);
        if (!found)
            return Json(new { success = false, message = "Task not found." });

        return Json(new { success = true, activityId });
    }

    [HttpGet]
    public async Task<IActionResult> GetEligiblePresentationStudents(int taskId, bool includeAlreadyAssigned = false)
    {
        var eligible = await _taskService.GetEligiblePresentationStudentsAsync(taskId, includeAlreadyAssigned);
        if (eligible == null) return NotFound();

        return Json(eligible);
    }
}
