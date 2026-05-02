using Tutorx.Web.Models.DTOs;

namespace Tutorx.Web.Services;

public interface IDrawService
{
    Task<int> GetNextDrawBatchIdAsync(int groupId);
    Task<List<DrawHistoryDto>> GetHistoryAsync(int groupId, int limit = 50);
    Task<List<DrawBatchDto>> GetBatchHistoryAsync(int groupId);
}
