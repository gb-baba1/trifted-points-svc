using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models;
using Trifted.Points.Data.Entities.WdrbeQuest;

namespace Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Interfaces;

public interface IWdrbeQuestManager
{
    public Task<CreateWdrbeQuestResponse?> CreateWdrbeQuestAsync(CreateWdrbeQuestRequest? request, Guid userId);
    public Task<GetWdrbeQuestTasksResponse?> WdrbeQuestTaskByIdAsync(string taskId, string questId);
    public Task<WdrbeQuestResponse?> GetQuestByIdAsync(string questId);
    public Task<List<GetWbdrbeQuestResponse>> GetWdrbeQuestsAsync();
    public Task<WdrbeQuestEntity> ProcessUserPointAsync(Guid userId, Guid questId, Guid taskId);
    public Task<UserPointResponse> GetUsersQuestPointAsync(Guid userId);
}