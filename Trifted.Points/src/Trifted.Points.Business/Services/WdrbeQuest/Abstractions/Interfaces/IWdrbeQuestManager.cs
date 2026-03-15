using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Dtos;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models;
using Trifted.Points.Data.Repositories;

namespace Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Interfaces;

public interface IWdrbeQuestManager
{
    public Task<CreateWdrbeQuestResponse?> CreateWdrbeQuestAsync(CreateWdrbeQuestDto? request, Guid userId);
    public Task<GetWbdrbeQuestResponse?> UpdateWdrbeQuestByIdAsync(UpdateWdrbeQuestDto request);
    Task<WdrbeQuestRepositoryItemCollection> GetItemCollectionAsync(string questId);
    public Task<GetWdrbeQuestTasksResonse?> WdrbeQuestTaskByIdAsync(string taskId, string questId);
    public Task<List<GetWdrbeQuestTasksResonse>> GetWdrbeQuestTasksAsync();
    public Task<WdrbeQuestResponse?> GetQuestByIdAsync(string questId);
    public Task<List<GetWbdrbeQuestResponse>> GetWdrbeQuestsAsync();
    public Task<GetWbdrbeQuestResponse?> RemoveWdrbeQuestByIdAsync(string questId);

}