using Trifted.Points.Business.Services.UserQuest.Abstractions.Dtos;
using Trifted.Points.Business.Services.UserStreak.Abstractions.Dtos;

namespace Trifted.Points.Business.Services.UserStreak.Abstractions.Interfaces;

public interface IUserStreakManager
{
    public Task ProcessUserStreakAsync(Guid userId);
    public Task<UserStreakResponse> GetUserCurrentStreak(Guid userId);

}