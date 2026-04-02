using Kanject.Core.Api.Abstractions.Exceptions;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Abstractions.Interfaces;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDbV2;
using Kanject.Core.SystemConsole.Extensions;

using Trifted.Points.Business.Services.UserQuest.Abstractions.Constants;
using Trifted.Points.Business.Services.UserStreak.Abstractions.Dtos;
using Trifted.Points.Business.Services.UserStreak.Abstractions.Interfaces;
using Trifted.Points.Data.DbContexts;
using Trifted.Points.Data.Entities.Users;
using Trifted.Points.Data.Repositories;


namespace Trifted.Points.Business.Services.UserStreak;

[RepositoryService(Repository = typeof(UserRepository))]
public partial class UserStreakManagerService(
    IUnitOfWork<TriftedPointsDbContext> unitOfWork)
    : Service<UserProfileEntity>(unitOfWork), IUserStreakManagerService
{
    public async Task ProcessUserStreakAsync(Guid userId)
    {
        try
        {
            var userCollection = await Repository.GetItemCollectionAsync(userId);

            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            var userStreaks = userCollection?.UserStreaks ?? [];

            if (userStreaks.Any(x => x.Date.Date == today))
                return;

            var latest = userStreaks
                .OrderByDescending(x => x.Date)
                .FirstOrDefault();

            int currentStreak = 1;
            int longestStreak = latest?.LongestStreak ?? 0;

            if (latest != null)
            {
                var lastDate = latest.Date.Date;

                if (lastDate == yesterday)
                {
                    currentStreak = latest.CurrentStreak + 1;
                }
                else if (lastDate < yesterday)
                {
                    latest.IsBroken = true;
                    latest.BrokenOn = today;

                    await Repository.UserStreaks.AddOrUpdateAsync(latest);
                }

                longestStreak = Math.Max(longestStreak, currentStreak);
            }

            var newStreak = new UserStreakEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = today,
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak,
                IsBroken = false
            };

            Repository.BeginTransaction();

            await Repository.UserStreaks.AddOrUpdateAsync(newStreak);

            await Repository.CommitAsync();
        }
        catch (Exception ex) when (ex is not ApiServiceException)
        {
            ex.PrintInConsole(tag: nameof(ProcessUserStreakAsync));
            throw new ApiServiceException(UserQuestManagerMessages.SystemCouldNotProcessUserQuest, ex);
        }
    }
    public async Task<UserStreakResponse> GetUserCurrentStreak(Guid userId)
    {
        var userCollection = await Repository.GetItemCollectionAsync(userId);

        var streaks = userCollection?.UserStreaks ?? [];

        var latest = streaks
            .OrderByDescending(x => x.Date)
            .FirstOrDefault();

        return new UserStreakResponse
        {
            CurrentStreak = latest?.CurrentStreak ?? 0,
            LongestStreak = latest?.LongestStreak ?? 0,
            Date = latest?.Date ?? DateTime.MinValue,
            IsBroken = latest?.IsBroken ?? true
        };
    }
}