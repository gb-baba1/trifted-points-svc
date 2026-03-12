using Amazon.DynamoDBv2.Model;
using Kanject.Core.Api.Abstractions.Exceptions;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Abstractions.Interfaces;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDbV2;
using Kanject.Core.Queue.Abstractions.Interfaces;
using Kanject.Core.SystemConsole.Extensions;
using Kanject.ServerlessEventHub.Provider.AwsSns.Abstractions.DataStore;
using Trifted.Core.Trifted.Identity.Queues;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Constants;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Interfaces;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models;
using Trifted.Points.Data.DbContexts;
using Trifted.Points.Data.Entities;
using Trifted.Points.Data.Entities.Users;
using Trifted.Points.Data.Entities.WdrbeQuest;
using Trifted.Points.Data.Repositories;
namespace Trifted.Points.Business.Services.WdrbeQuest;

[RepositoryService(Repository = typeof(WdrbeQuestRepository))]
public partial class WdrbeQuestManagerService(
    IServerlessEventHubDataStore serverlessEventHubDataStore,
    IQueueManagerService queueManagerService,
    UserQuestRepository userQuestRepository,
    UserQuestTaskRepository userQuestTaskRepository,
    IUnitOfWork<TriftedPointsDbContext> unitOfWork)
    : Service<WdrbeQuestEntity>(unitOfWork), IWdrbeQuestManagerService
{
    public async Task<CreateWdrbeQuestResponse?> CreateWdrbeQuestAsync(CreateWdrbeQuestRequest? request, Guid userId)
    {
        try
        {
            if (request is null)
                throw new ApiValidationException(WdrbeQuestManagerMessages.ModelIsRequired);

            string[] questEventTopics = [..request.Tasks.Select(task => task.EventTopic)];

            var eventTopics = (await serverlessEventHubDataStore.GetServiceTopicsByNameAsync(topic:
                                  questEventTopics))
                              ?? throw new ApiValidationException(WdrbeQuestManagerMessages.EventTopicNotFound);

            if (eventTopics.Count() != questEventTopics.Length)
            {
                var foundEventTopicNames = eventTopics.Select(topic => topic.Topic).ToHashSet();
                var notFoundEventTopics = questEventTopics.Where(topic => !foundEventTopicNames.Contains(topic));
                throw new ApiValidationException(
                    WdrbeQuestManagerMessages.EventTopicNotFound.Replace("{{event-topic}}",
                        string.Join(", ", notFoundEventTopics)));
            }

            var topicLookup = eventTopics.ToDictionary(t => t.Topic);
            int questPoint = 0;
            foreach (var task in request.Tasks)
            {
                if (!topicLookup.TryGetValue(task.EventTopic, out var topic))
                {
                    throw new ApiValidationException(
                        WdrbeQuestManagerMessages.EventTopicNotFound
                            .Replace("{{event-topic}}", task.EventTopic));
                }

                if (!topic.Parameters.Contains(task.UserIdentifier))
                {
                    throw new ApiValidationException(
                        WdrbeQuestManagerMessages.UserIdentifierNotFound
                            .Replace("{{event-topic}}", task.EventTopic));
                }
                questPoint += (task.PointPerAction * task.MaxAction);
            }

            var succeeded = await queueManagerService.SubscribeWdrbeQuestQueueToTopicsAsync(topics:
                [..eventTopics.Select(topic => topic.Topic)]);

            if (!succeeded)
                throw new ApiServiceException(WdrbeQuestManagerMessages.SystemCouldNotCreateQuest);

            return (CreateWdrbeQuestResponse?)await ProcessQuestCreationAsync(request,questPoint);
        }
        catch (TransactionCanceledException ex)
        {
            ex.PrintInConsole(tag: nameof(CreateWdrbeQuestAsync));
            throw new ApiValidationException(WdrbeQuestManagerMessages.QuestHasBeenCreated);
        }
        catch (Exception ex) when (ex is not ApiServiceException)
        {
            throw new ApiServiceException(WdrbeQuestManagerMessages.SystemCouldNotCreateQuest);
        }
    }

    private async Task<WdrbeQuestEntity> ProcessQuestCreationAsync(CreateWdrbeQuestRequest request, int questPoint)
    {
        if (!await Repository.IsWdrbeQuestQuestNameUniqueAsync(
                name: request.QuestName.StripFormat()))
            throw new ApiValidationException(WdrbeQuestManagerMessages.EventTopicAlreadyCreated);

        var currentDateTimeAsString = CurrentDateTimeAsString;
        var quest = new WdrbeQuestEntity
        {
            Id = Guid.NewGuid(),
            CountryId = request.CountryId,
            Name = request.QuestName.StripFormat(),
            Description = request.QuestName.StripFormat(),
            Points = questPoint,
            IsActive = request.IsActive,
            Badge = request.Badge,
            CreatedOn = currentDateTimeAsString,
            LastUpdatedOn = currentDateTimeAsString
        };
        var newQuestCollection = new WdrbeQuestRepositoryItemCollection
        {
            WdrbeQuest = quest,
            WdrbeQuestTask =  request.Tasks
                .Select(task => new WdrbeQuestTaskEntity
                {
                    Id = Guid.NewGuid(),
                    QuestId = quest.Id,
                    Name = task.TaskName,
                    CountryId = quest.CountryId,
                    PointPerAction = task.PointPerAction,
                    EventTopic = task.EventTopic,
                    MaxAction = task.MaxAction,
                    UserIdentifier = task.UserIdentifier,
                    CreatedOn = currentDateTimeAsString,
                    LastUpdatedOn = currentDateTimeAsString
                })
                .OrderBy(item => item.Name)
                .First()
        };

        Repository.BeginTransaction();
        await Repository.InsertItemCollectionAsync(newQuestCollection);
        await Repository.CommitAsync();

        return quest;
    }

    public async Task<GetWdrbeQuestTasksResponse?> WdrbeQuestTaskByIdAsync(string taskId, string questId)
    {
        try
        {
            if (string.IsNullOrEmpty(taskId))
                throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidTaskId);

            if (!Guid.TryParse(taskId, out var taskGUid))
                throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidTaskId);

            if (string.IsNullOrEmpty(questId))
                throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidQuestId);

            if (!Guid.TryParse(questId, out var questGUid))
                throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidQuestId);

            var task = await Repository.WdrbeQuestTask.FindWdrbeQuestTaskAsync(questGUid, taskGUid);
            return task is null
                ? throw new ApiValidationException(WdrbeQuestManagerMessages.TaskNotFound)
                : new GetWdrbeQuestTasksResponse
                {
                    TaskName = task.Name,
                    MaxAction = task.MaxAction,
                    PointPerAction = task.PointPerAction,
                    CountryId = task.CountryId,
                    LastUpdatedOn = task.LastUpdatedOn,
                    Points = (task.MaxAction * task.PointPerAction)
                };
        }
        catch (Exception ex) when (ex is not ApiServiceException)
        {
            ex.PrintInConsole(tag: nameof(WdrbeQuestTaskByIdAsync));
            throw new ApiServiceException(WdrbeQuestManagerMessages.SystemCouldNotGetQuest);
        }
    }
    public async Task<WdrbeQuestResponse?> GetQuestByIdAsync(string questId)
    {
        try
        {
            if (!Guid.TryParse(questId, out var questGuid))
                throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidQuestId);

            var questItemCollection = await Repository.GetItemCollectionAsync(questGuid)
                                       ?? throw new ApiValidationException(WdrbeQuestManagerMessages.QuestNotFound);

            if (questItemCollection.WdrbeQuest is null)
                throw new ApiValidationException(WdrbeQuestManagerMessages.QuestNotFound);

            if (questItemCollection.WdrbeQuestTask is null)
                throw new ApiValidationException(WdrbeQuestManagerMessages.TaskNotFound);

            return new WdrbeQuestResponse
            {
                QuestId = questItemCollection.WdrbeQuest.Id.ToString(),
                QuestName = questItemCollection.WdrbeQuest.Name,
                QuestDescription = questItemCollection.WdrbeQuest.Description,
                Badge = questItemCollection.WdrbeQuest.Badge,
                IsActive = questItemCollection.WdrbeQuest.IsActive,
                TotalPoints = questItemCollection.WdrbeQuest.Points,
                CountryId = questItemCollection.WdrbeQuest.CountryId,
                LastUpdatedOn = questItemCollection.WdrbeQuest.LastUpdatedOn,
                //TaskQuest =
                //[
                //    ..questItemCollection.WdrbeQuestTask.Select(item => new GetWdrbeQuestTasksResponse
                //            {
                //                MaxAction = item.MaxAction,
                //                TaskId = item.Id,
                //                TaskName = item.Name,
                //                CountryId = item.CountryId,
                //                PointPerAction = item.PointPerAction,
                //                Points = (item.Max * item.PointPerAction),
                //                CountryId = item.CountryId, 
                //                LastUpdatedOn = item.LastUpdatedOn,
                //            }
                //        )?.OrderBy(item => item.ItemDefaultImageUrl)
                //        ?.ThenBy(item => item.ItemName)
                //        ?.ToArray() ?? []
                //]
            };
        }
        catch (Exception ex) when (ex is not ApiServiceException)
        {
            throw new ApiServiceException(WdrbeQuestManagerMessages.SystemCouldNotCreateQuest);
        }
    }

    public async Task<List<GetWbdrbeQuestResponse>> GetWdrbeQuestsAsync()
    {
        try
        {
            var quests = await Repository.FindWdrbeQuestsUsingGsi1IndexAsync();
            return
            [
                .. quests.Select(g => new GetWbdrbeQuestResponse
                {
                    TotalPoints = g.Points,
                    IsActive = g.IsActive,
                    Badge = g.Badge,
                    CountryId = g.CountryId,
                    QuestId = g.Id.ToString(),
                    LastUpdatedOn = g.LastUpdatedOn,
                    QuestDescription= g.Description,
                    QuestName = g.Name
                })
            ];
        }
        catch (Exception ex) when (ex is not ApiServiceException)
        {
            ex.PrintInConsole(tag: nameof(GetWdrbeQuestsAsync));
            throw new ApiServiceException(WdrbeQuestManagerMessages.SystemCouldNotGetQuest);
        }
    }

    public async Task<UserPointResponse> GetUsersQuestPointAsync(Guid userId)
    {
        try
        {
            var allQuestsTask = Repository.FindWdrbeQuestsUsingGsi1IndexAsync();
            var userQuestsTask = userQuestTaskRepository.UserQuest.FindUserQuestsAsync(userId);
            var userTasksTask = userQuestTaskRepository.FindUserQuestTasksAsync(userId);

            await Task.WhenAll(allQuestsTask, userQuestsTask, userTasksTask);

            var allQuests = await allQuestsTask;
            var userQuests = await userQuestsTask;
            var userTasks = await userTasksTask;

            if (allQuests == null || allQuests.Count == 0)
            {
                return new UserPointResponse();
            }

            var taskIds = userTasks.Select(t => t.TaskId).Distinct().ToHashSet();

            var taskNameLookup = new Dictionary<Guid, string>();

            if (taskIds.Count != 0)
            {
                foreach (var taskId in taskIds)
                {
                    var task = await Repository.WdrbeQuestTask.FindWdrbeQuestTaskAsync(taskId.ToString());
                    if (task != null)
                    {
                        taskNameLookup[task.Id] = task.Name;
                    }
                }
            }

            var totalUserPoints = userQuests.Sum(q => q.Points);
            var tasksByQuest = userTasks.GroupBy(t => t.QuestId)
                                        .ToDictionary(g => g.Key, g => g.ToList());
            var totalPossiblePoints = allQuests.Sum(q => q.Points);

            var questResponses = allQuests.Select(quest =>
            {
                var userQuest = userQuests.FirstOrDefault(x => x.QuestId == quest.Id);
                var pointsEarned = userQuest?.Points ?? 0;

                var questTasks = tasksByQuest.TryGetValue(quest.Id, out var tasks)
                    ? tasks.Select(t => new UserQuestTaskResponse
                    {
                        TaskName = taskNameLookup.TryGetValue(t.TaskId, out var name)
                            ? name
                            : string.Empty,
                        PointsEarned = t.Points,
                        IsCompleted = t.IsCompleted
                    }).ToList()
                    : [];

                return new UserQuestResponse
                {
                    QuestName = quest.Name,
                    QuestDescription = quest.Description,
                    TotalPoints = quest.Points,
                    PointsEarned = pointsEarned,
                    CompletionPercentage = quest.Points == 0
                        ? 0
                        : (int)((double)pointsEarned / quest.Points * 100),
                    IsCompleted = pointsEarned >= quest.Points,
                    QuestTasks = questTasks
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
            throw new ApiServiceException(WdrbeQuestManagerMessages.SystemCouldNotGetQuest, ex);
        }
    }


    public async Task<WdrbeQuestEntity> ProcessUserPointAsync(Guid userId, Guid questId, Guid taskId)
    {
        var questTaskTask = Repository.WdrbeQuestTask.FindWdrbeQuestTaskAsync(questId, taskId);
        var questTask = Repository.FindWdrbeQuestAsync(questId);

        var userQuestTask = userQuestTaskRepository.UserQuest.FindUserQuestAsync(userId, questId);
        var userQuestTaskTask = userQuestTaskRepository.FindUserQuestTaskAsync(userId, questId, taskId);

        var userPointTask = userQuestRepository.UserPoint.FindUserPointAsync(userId);

        await Task.WhenAll(questTaskTask, questTask, userQuestTask, userQuestTaskTask, userPointTask);

        var quest = questTask.Result ?? throw new ApiValidationException(WdrbeQuestManagerMessages.QuestNotFound);
        var task = questTaskTask.Result ?? throw new ApiValidationException(WdrbeQuestManagerMessages.TaskNotFound);

        var userQuest = userQuestTask.Result;
        var userTask = userQuestTaskTask.Result;
        var userPoint = userPointTask.Result;

        var pointsPerAction = task.PointPerAction;
        var maxTaskPoints = task.PointPerAction * task.MaxAction;


        userPoint ??= new UserPointEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Points = 0
        };

        userPoint.Points += pointsPerAction;

        userQuest ??= new UserQuestEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QuestId = questId,
            Points = 0
        };

        if (userQuest.Points < quest.Points)
        {
            var remainingQuestPoints = quest.Points - userQuest.Points;
            var questPointsToAdd = Math.Min(pointsPerAction, remainingQuestPoints);

            userQuest.Points += questPointsToAdd;
        }
        else
        {
            ($"User {userId} already completed quest {questId}").PrintInConsole();
        }


        userTask ??= new UserQuestTaskEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QuestId = questId,
            TaskId = task.Id,
            Points = 0
        };

        if (userTask.Points < maxTaskPoints)
        {
            var remainingTaskPoints = maxTaskPoints - userTask.Points;
            var taskPointsToAdd = Math.Min(pointsPerAction, remainingTaskPoints);

            userTask.Points += taskPointsToAdd;
        }
        else
        {
            ($"User {userId} already completed task {task.Id}").PrintInConsole();
        }

        userQuestTaskRepository.BeginTransaction();

        await userQuestTaskRepository.UserQuest.AddOrUpdateAsync(userQuest);
        await userQuestTaskRepository.AddOrUpdateAsync(userTask);

        await userQuestRepository.UserPoint.AddOrUpdateAsync(userPoint);

        await userQuestTaskRepository.CommitAsync();
        await userQuestRepository.CommitAsync();

        return quest;
    }
}