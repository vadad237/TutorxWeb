using Tutorx.Web.Models.Entities;

namespace Tutorx.Web.Models.DTOs;

public record AssignmentResultDto(List<Assignment> Assignments, int TotalStudents, int TotalTasks);
