namespace Tutorx.Web.Models.DTOs;

public record BulkSetActiveRequest(int[] StudentIds, bool Active);
