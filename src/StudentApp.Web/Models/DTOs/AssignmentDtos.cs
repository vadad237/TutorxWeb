using StudentApp.Web.Models.Entities;

namespace StudentApp.Web.Models.DTOs;

public record AssignmentResultDto(List<Assignment> Assignments, int TotalStudents, int TotalTasks);
