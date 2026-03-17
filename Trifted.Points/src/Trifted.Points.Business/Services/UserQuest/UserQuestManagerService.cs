using Kanject.Core.Api.Abstractions.Exceptions;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Abstractions.Interfaces;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDbV2;
using Kanject.Core.SystemConsole.Extensions;

using Trifted.Points.Business.Services.UserQuest.Abstractions.Constants;
using Trifted.Points.Business.Services.UserQuest.Abstractions.Dtos;
using Trifted.Points.Business.Services.UserQuest.Abstractions.Interfaces;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Dtos;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Interfaces;
using Trifted.Points.Data.DbContexts;
using Trifted.Points.Data.Entities.Users;
using Trifted.Points.Data.Repositories;


namespace Trifted.Points.Business.Services.UserQuest;

[RepositoryService(Repository = typeof(UserRepository))]
public partial class UserQuestManagerService(
    IWdrbeQuestManagerService wdrbeQuestManagerService,
    IUnitOfWork<TriftedPointsDbContext> unitOfWork)
    : Service<UserProfileEntity>(unitOfWork), IUserQuestManagerService
{
    public async Task<UserQuestResponse> ProcessUserPointAsync(Guid userId, Guid questId, Guid taskId)
    {
        try
        {
            var questCollectionTask = wdrbeQuestManagerService.GetItemCollectionAsync(questId.ToString());
            var userCollectionTask = Repository.GetItemCollectionAsync(userId);

            await Task.WhenAll(questCollectionTask, userCollectionTask);

            var questCollection = questCollectionTask.Result;
            var userCollection = userCollectionTask.Result;

            var quest = questCollection.WdrbeQuest;

            var task = questCollection.WdrbeQuestTasks
                .FirstOrDefault(x => x.Id == taskId);

            var userQuests = userCollection?.UserQuests ?? [];
            var userTasks = userCollection?.UserQuestTasks ?? [];
            var userPoint = userCollection?.UserPoint;

            var userQuest = userQuests.FirstOrDefault(q => q.QuestId == questId);

            var userTask = userTasks.FirstOrDefault(t => t.QuestId == questId && t.TaskId == taskId);

            var pointsPerAction = task!.PointPerAction;
            var maxTaskPoints = task.PointPerAction * task.MaxAction;

            if (userTask?.IsCompleted == true)
            {
                return new UserQuestResponse
                {
                    Badge = userQuest!.Badge,
                    TotalPoints = userQuest?.Points ?? 0
                };
            }

            userPoint ??= new UserPointEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Points = 0
            };

            userQuest ??= new UserQuestEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuestId = questId,
                Points = 0
            };

            userTask ??= new UserQuestTaskEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuestId = questId,
                TaskId = task.Id,
                Points = 0,
                IsCompleted = false
            };

            var remainingTaskPoints = maxTaskPoints - userTask.Points;
            var pointsToAward = Math.Min(pointsPerAction, remainingTaskPoints);

            if (pointsToAward <= 0)
            {
                return new UserQuestResponse
                {
                    Badge = userQuest.Badge,
                    TotalPoints = userQuest.Points
                };
            }

            userTask.Points += pointsToAward;

            if (userTask.Points >= maxTaskPoints && !userTask.IsCompleted)
            {
                userTask.IsCompleted = true;
                userTask.CompletedOn = DateTime.UtcNow;
            }

            if (userQuest.Points < quest?.Points)
            {
                var remainingQuestPoints = quest.Points - userQuest.Points;
                userQuest.Points += Math.Min(pointsToAward, remainingQuestPoints);
            }

            userPoint.Points += pointsToAward;

            if (userQuest.Points >= quest?.Points && quest.Badge != Data.Enums.Badge.NoBadge)
            {
                userQuest.Badge = quest.Badge;
                userPoint.Badge = quest.Badge;
            }

            Repository.BeginTransaction();

            await Repository.UserQuests.AddOrUpdateAsync(userQuest);
            await Repository.UserQuestTasks.AddOrUpdateAsync(userTask);
            await Repository.UserPoint.AddOrUpdateAsync(userPoint);

            await Repository.CommitAsync();

            return new UserQuestResponse
            {
                Badge = userQuest.Badge,
                TotalPoints = userQuest.Points
            };
        }
        catch (Exception ex) when (ex is not ApiServiceException)
        {
            ex.PrintInConsole(tag: nameof(ProcessUserPointAsync));
            throw new ApiServiceException(UserQuestManagerMessages.SystemCouldNotProcessUserQuest, ex);
        }
    }
    public async Task<UserPointResponse> GetUsersQuestPointAsync(Guid userId)
    {
        try
        {
            var questsTask = wdrbeQuestManagerService.GetWdrbeQuestsAsync();
            var userCollectionTask = Repository.GetItemCollectionAsync(userId);

            await Task.WhenAll(questsTask, userCollectionTask);

            var allQuests = await questsTask;
            var userCollection = await userCollectionTask;

            if (allQuests == null || allQuests.Count == 0)
            {
                return new UserPointResponse();
            }

            var questIds = allQuests.Select(q => q.QuestId).ToList();

            var allTasks = await wdrbeQuestManagerService.GetTasksByQuestIdsAsync(questIds);

            var tasksByQuestId = allTasks
                .GroupBy(t => t.QuestId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var userQuests = userCollection?.UserQuests ?? [];
            var userTasks = userCollection?.UserQuestTasks ?? [];

            var userQuestLookup = userQuests.ToDictionary(q => q.QuestId);

            var userTasksByQuestAndTask = userTasks
                .GroupBy(t => t.QuestId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(t => t.TaskId, t => t)
                );

            var totalUserPoints = userQuests.Sum(q => q.Points);

            var questResponses = allQuests.Select(quest =>
            {
                var pointsEarned = userQuestLookup.TryGetValue(quest.QuestId, out var userQuest)
                    ? userQuest.Points
                    : 0;

                var questTasks = tasksByQuestId.TryGetValue(quest.QuestId, out var tasks)
                    ? tasks
                    : [];

                var userTasksForQuest = userTasksByQuestAndTask.TryGetValue(quest.QuestId, out var userTaskDict)
                    ? userTaskDict
                    : [];

                var taskResponses = questTasks
                    .Select(task =>
                    {
                        var userTask = userTasksForQuest.TryGetValue(task.TaskId, out var ut) ? ut : null;

                        var taskPoints = userTask?.Points ?? 0;
                        var maxTaskPoints = task.PointPerAction * task.MaxAction;
                        var completionPercentage = maxTaskPoints == 0
                            ? 0
                            : Math.Round((decimal)taskPoints / maxTaskPoints * 100, 2);

                        return new UserQuestTaskResponse
                        {
                            TaskName = task.TaskName,
                            TotalPoints = maxTaskPoints,
                            PointsEarned = taskPoints,
                            CompletionPercentage = completionPercentage,
                            IsCompleted = userTask?.IsCompleted ?? false
                        };
                    })
                    .OrderBy(t => t.TaskName)
                    .ToList();

                return new UserQuestResponse
                {
                    QuestName = quest.QuestName,
                    QuestDescription = quest.QuestDescription,
                    TotalPoints = quest.TotalPoints,
                    PointsEarned = pointsEarned,
                    CompletionPercentage = quest.TotalPoints == 0
                        ? 0
                        : (int)((double)pointsEarned / quest.TotalPoints * 100),
                    IsCompleted = pointsEarned >= quest.TotalPoints,
                    Badge = pointsEarned >= quest.TotalPoints
                        ? quest.Badge
                        : Data.Enums.Badge.NoBadge,
                    QuestTasks = taskResponses
                };
            }).ToList();

            return new UserPointResponse
            {
                PointsEarned = totalUserPoints,
                Quests = questResponses
            };
        }
        catch (Exception ex) when (ex is not ApiServiceException)
        {
            ex.PrintInConsole(tag: nameof(GetUsersQuestPointAsync));
            throw new ApiServiceException(UserQuestManagerMessages.SystemCouldNotGetUserQuest, ex);
        }
    }

}