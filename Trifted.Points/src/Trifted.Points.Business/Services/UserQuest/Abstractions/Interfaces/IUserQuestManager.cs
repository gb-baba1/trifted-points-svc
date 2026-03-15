using Trifted.Points.Business.Services.UserQuest.Abstractions.Dtos;

namespace Trifted.Points.Business.Services.UserQuest.Abstractions.Interfaces;

public interface IUserQuestManager
{
    public Task<UserQuestResponse> ProcessUserPointAsync(Guid userId, Guid questId, Guid taskId);
    public Task<UserPointResponse> GetUsersQuestPointAsync(Guid userId);

}