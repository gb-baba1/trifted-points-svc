using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models;

namespace Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Interfaces;

public interface IWdrbeQuestManager
{
    public Task<List<WdrbeQuestResponse>?> WdrbeQuestsAsync();

    public Task<CreateWdrbeQuestResponse?> CreateWdrbeQuestAsync(CreateWdrbeQuestRequest? request, Guid userId);
}