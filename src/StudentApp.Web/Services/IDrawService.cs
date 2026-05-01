using StudentApp.Web.Models.DTOs;

namespace StudentApp.Web.Services;

public interface IDrawService
{
    Task<DrawResultDto> DrawNextAsync(int groupId);
    Task<int> GetNextDrawBatchIdAsync(int groupId);
    Task<List<DrawHistoryDto>> GetHistoryAsync(int groupId, int limit = 50);
    Task<List<DrawBatchDto>> GetBatchHistoryAsync(int groupId);
    Task<BagStatusDto> GetBagStatusAsync(int groupId);
    Task ResetBagAsync(int groupId);
}
