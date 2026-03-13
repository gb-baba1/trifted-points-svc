using Amazon.DynamoDBv2.Model;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime.Internal;
using Kanject.Core.Api.Abstractions.Exceptions;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Abstractions.Interfaces;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDbV2;
using Kanject.Core.Queue.Abstractions.Interfaces;
using Kanject.Core.Queue.Abstractions.Models;
using Kanject.Core.SystemConsole.Extensions;
using Kanject.ServerlessEventHub.Provider.AwsSns.Abstractions.DataStore;
using Trifted.Core.Trifted.Identity.Queues;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Constants;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Interfaces;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models;
using Trifted.Points.Common.Constants;
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
    IUnitOfWork<TriftedPointsDbContext> unitOfWork)
    : Service<WdrbeQuestEntity>(unitOfWork), IWdrbeQuestManagerService
{
    public async Task<CreateWdrbeQuestResponse?> CreateWdrbeQuestAsync(CreateWdrbeQuestRequest? request, Guid userId)
    {
        try
        {
            if (request is null)
                throw new ApiValidationException(WdrbeQuestManagerMessages.ModelIsRequired);

            string[] questEventTopics = [.. request.Tasks.Select(task => task.EventTopic)];

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
                var totalTaskPoints = task.PointPerAction * task.MaxAction;
                questPoint += totalTaskPoints;
            }

            //var succeeded = await queueManagerService.SubscribeWdrbeQuestQueueToTopicsAsync(topics:
            //    [..eventTopics.Select(topic => topic.Topic)]);

            //if (!succeeded)
            //    throw new ApiServiceException(WdrbeQuestManagerMessages.SystemCouldNotCreateQuest);

            return (CreateWdrbeQuestResponse?)await ProcessQuestCreationAsync(request, questPoint);
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
        var questName = request.QuestName.StripFormat();

        if (!await Repository.IsWdrbeQuestQuestNameUniqueAsync(name: questName))
            throw new ApiValidationException(WdrbeQuestManagerMessages.EventTopicAlreadyCreated);

        var currentDateTimeAsString = CurrentDateTimeAsString;
        var questId = Guid.NewGuid();
        var countryId = request.CountryId;
        var tasks = request.Tasks;
        var questDescription = request.QuestDescription.StripFormat();

        var newQuestCollection = new WdrbeQuestRepositoryItemCollection
        {
            WdrbeQuest = new WdrbeQuestEntity
            {
                Id = questId,
                CountryId = countryId,
                Name = questName,
                Description = questDescription,
                Points = questPoint,
                IsActive = request.IsActive,
                Badge = request.Badge,
                CreatedOn = currentDateTimeAsString,
                LastUpdatedOn = currentDateTimeAsString
            },
            WdrbeQuestTasks = [.. tasks
                .Select(task =>
                {
                    var taskId = Guid.NewGuid();
                    var totalPoints = task.PointPerAction * task.MaxAction;

                    return new WdrbeQuestTaskEntity
                    {
                        Id = taskId,
                        QuestId = questId,
                        Name = task.TaskName,
                        CountryId = countryId,
                        PointPerAction = task.PointPerAction,
                        EventTopic = task.EventTopic,
                        MaxAction = task.MaxAction,
                        Points = totalPoints,
                        UserIdentifier = task.UserIdentifier,
                        CreatedOn = currentDateTimeAsString,
                        LastUpdatedOn = currentDateTimeAsString
                    };
                })
                .OrderBy(x => x.Name)]
        };

        Repository.BeginTransaction();
        await Repository.InsertItemCollectionAsync(newQuestCollection);
        await Repository.CommitAsync();

        return newQuestCollection.WdrbeQuest;
    }

    public async Task<GetWdrbeQuestTasksResponse?> WdrbeQuestTaskByIdAsync(string taskId, string questId)
    {
        try
        {
            if (string.IsNullOrEmpty(taskId))
                throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidTaskId);

            if (!Guid.TryParse(taskId, out var taskGuid))
                throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidTaskId);

            if (string.IsNullOrEmpty(questId))
                throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidQuestId);

            if (!Guid.TryParse(questId, out var questGuid))
                throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidQuestId);

            var task = await Repository.WdrbeQuestTasks.FindWdrbeQuestTaskAsync(questGuid, taskGuid);
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
    public async Task<GetWbdrbeQuestResponse?> RemoveWdrbeQuestByIdAsync(string questId)
    {
        try
        {
            if (string.IsNullOrEmpty(questId))
                throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidQuestId);

            if (!Guid.TryParse(questId, out var questGuid))
                throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidQuestId);

            var questItemCollection = await Repository.GetItemCollectionAsync(questGuid) ?? throw new ApiValidationException(WdrbeQuestManagerMessages.QuestNotFound);

            if (questItemCollection.WdrbeQuest is null)
                throw new ApiValidationException(WdrbeQuestManagerMessages.QuestNotFound);

            Repository.BeginTransaction();

            await Repository.RemoveItemCollectionAsync(questItemCollection);

            await Repository.CommitAsync();

            return new GetWbdrbeQuestResponse
            {
                QuestName = questItemCollection.WdrbeQuest.ToString(),
                QuestDescription = questItemCollection.WdrbeQuest.Description,
                LastUpdatedOn = questItemCollection.WdrbeQuest.LastUpdatedOn,
                TotalPoints = questItemCollection.WdrbeQuest.Points,
                IsActive = questItemCollection.WdrbeQuest.IsActive,
                Badge = questItemCollection.WdrbeQuest.Badge
            };
        }
        catch (Exception ex) when (ex is not ApiServiceException)
        {
            ex.PrintInConsole(tag: nameof(WdrbeQuestTaskByIdAsync));
            throw new ApiServiceException(WdrbeQuestManagerMessages.SystemCouldNotGetQuest);
        }
    }
    public async Task<GetWbdrbeQuestResponse?> UpdateWdrbeQuestByIdAsync(UpdateWdrbeQuestRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.QuestId))
                throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidQuestId);

            if (!Guid.TryParse(request.QuestId, out var questGuid))
                throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidQuestId);

            var questItemCollection = await Repository.GetItemCollectionAsync(questGuid)
                ?? throw new ApiValidationException(WdrbeQuestManagerMessages.QuestNotFound);

            var quest = questItemCollection.WdrbeQuest
                ?? throw new ApiValidationException(WdrbeQuestManagerMessages.QuestNotFound);

            var existingTasks = questItemCollection.WdrbeQuestTasks ?? new List<WdrbeQuestTaskEntity>();

            Repository.BeginTransaction();

            var now = CurrentDateTimeAsString;

            var requestTasks = request.Tasks
                .Where(x => !string.IsNullOrWhiteSpace(x.TaskId))
                .ToDictionary(x => Guid.Parse(x.TaskId));

            var existingTaskIds = existingTasks.Select(x => x.Id).ToHashSet();

            var tasksToUpdate = existingTasks
                .Where(t => requestTasks.ContainsKey(t.Id))
                .Select(existing =>
                {
                    var req = requestTasks[existing.Id];

                    return existing with
                    {
                        Name = req.TaskName,
                        PointPerAction = req.PointPerAction,
                        MaxAction = req.MaxAction,
                        EventTopic = req.EventTopic,
                        UserIdentifier = req.UserIdentifier,
                        CountryId = req.CountryId,
                        Points = req.PointPerAction * req.MaxAction,
                        LastUpdatedOn = now
                    };
                })
                .ToList();

            var tasksToCreate = request.Tasks
                .Where(t => string.IsNullOrWhiteSpace(t.TaskId) || !existingTaskIds.Contains(Guid.Parse(t.TaskId)))
                .Select(t => new WdrbeQuestTaskEntity
                {
                    Id = Guid.NewGuid(),
                    QuestId = quest.Id,
                    Name = t.TaskName,
                    CountryId = t.CountryId,
                    PointPerAction = t.PointPerAction,
                    MaxAction = t.MaxAction,
                    EventTopic = t.EventTopic,
                    UserIdentifier = t.UserIdentifier,
                    Points = t.PointPerAction * t.MaxAction,
                    CreatedOn = now,
                    LastUpdatedOn = now
                })
                .ToList();

            var tasksToDelete = existingTasks
                .Where(t => !requestTasks.ContainsKey(t.Id))
                .ToList();

            var updatedPoints = tasksToUpdate.Sum(x => x.Points) + tasksToCreate.Sum(x => x.Points);

            var updatedQuest = quest with
            {
                Name = request.QuestName.StripFormat(),
                Description = request.QuestDescription.StripFormat(),
                IsActive = request.IsActive,
                CountryId = request.CountryId,
                Points = updatedPoints,
                LastUpdatedOn = now
            };

            await Repository.UpdateWithIndexAsync(updatedQuest, quest);

            foreach (var updatedTask in tasksToUpdate)
            {
                var original = existingTasks.First(x => x.Id == updatedTask.Id);
                await Repository.WdrbeQuestTasks.UpdateWithIndexAsync(updatedTask, original);
            }

            foreach (var newTask in tasksToCreate)
            {
                await Repository.WdrbeQuestTasks.InsertWithIndexAsync(newTask);
            }

            foreach (var deleteTask in tasksToDelete)
            {
                await Repository.WdrbeQuestTasks.RemoveWithIndexAsync(deleteTask);
            }

            await Repository.CommitAsync();

            return new GetWbdrbeQuestResponse
            {
                QuestName = updatedQuest.Name,
                QuestDescription = updatedQuest.Description,
                LastUpdatedOn = updatedQuest.LastUpdatedOn,
                TotalPoints = updatedQuest.Points,
                IsActive = updatedQuest.IsActive,
                Badge = updatedQuest.Badge
            };
        }
        catch (Exception ex) when (ex is not ApiServiceException)
        {
            ex.PrintInConsole(tag: nameof(UpdateWdrbeQuestByIdAsync));
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

            if (questItemCollection.WdrbeQuestTasks is null)
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
                TaskQuest =
                [
                    ..questItemCollection.WdrbeQuestTasks.Select(item => new GetWdrbeQuestTasksResponse
                            {
                                MaxAction = item.MaxAction,
                                TaskId = item.Id.ToString(),
                                TaskName = item.Name,
                                CountryId = item.CountryId,
                                EventTopic = item.EventTopic,
                                PointPerAction = item.PointPerAction,
                                Points = (item.MaxAction * item.PointPerAction),
                                LastUpdatedOn = item.LastUpdatedOn,
                            }
                        )?.OrderBy(item => item.TaskName)
                        ?.ThenBy(item => item.LastUpdatedOn)
                        ?.ToArray() ?? []
                ]
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
            var userQuestsTask = userQuestRepository.FindUserQuestsAsync(userId);
            var userTasksTask = userQuestRepository.UserQuestTask.FindUserQuestTasksAsync(userId);

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
                    var task = await Repository.WdrbeQuestTasks.FindWdrbeQuestTaskAsync(taskId.ToString());
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
                    Badge = (pointsEarned >= quest.Points) ? quest.Badge : Data.Enums.Badge.NoBadge,
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
        var questItemCollectionTask = Repository.GetItemCollectionAsync(questId);

        var userQuestTask = userQuestRepository.FindUserQuestAsync(userId, questId);

        var userQuestTaskTask = userQuestRepository.UserQuestTask.FindUserQuestTaskAsync(userId, questId, taskId);

        var userPointTask = userQuestRepository.UserPoint.FindUserPointAsync(userId);

        await Task.WhenAll(questItemCollectionTask, userQuestTask, userQuestTaskTask, userPointTask);

        var questResult = questItemCollectionTask.Result ?? throw new ApiValidationException(WdrbeQuestManagerMessages.QuestNotFound);

        var quest = questResult.WdrbeQuest ?? throw new ApiValidationException(WdrbeQuestManagerMessages.QuestNotFound);

        var task = questResult.WdrbeQuestTasks.FirstOrDefault(x => x.Id == taskId) ?? throw new ApiValidationException(WdrbeQuestManagerMessages.TaskNotFound);

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

        if (userQuest.Points >= quest.Points && quest.Badge != Data.Enums.Badge.NoBadge)
        {
            userQuest.Badge = quest.Badge;
            userPoint.Badge = quest.Badge;
        }
        userQuestRepository.BeginTransaction();

        await userQuestRepository.AddOrUpdateAsync(userQuest);
        await userQuestRepository.UserQuestTask.AddOrUpdateAsync(userTask);

        await userQuestRepository.UserPoint.AddOrUpdateAsync(userPoint);

        await userQuestRepository.CommitAsync();

        return quest;
    }
}