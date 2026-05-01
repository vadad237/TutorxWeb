using Tutorx.Web.Models.Entities;

namespace Tutorx.Web.Services;

public interface IActivityAttributeService
{
    Task<ActivityAttribute> CreateAttributeAsync(int activityId, string name);
    Task<bool> RenameAttributeAsync(int id, string name);
    Task<bool> DeleteAttributeAsync(int id);
    Task<ActivityAttributeOption> AddOptionAsync(int attributeId, string name, string color);
    Task<bool> EditOptionAsync(int id, string name, string color);
    Task<bool> DeleteOptionAsync(int id);
    Task SetValueAsync(int studentId, int attributeId, int? optionId);
}
