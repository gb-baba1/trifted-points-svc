using Amazon.DynamoDBv2.Model;

using Kanject.Core.Api.Abstractions.Exceptions;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Abstractions.Interfaces;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDbV2;
using Kanject.Core.SystemConsole.Extensions;

using Trifted.Points.Business.Services.QuestEventSubscription.Abstractions.Dtos;
using Trifted.Points.Business.Services.QuestEventSubscription.Abstractions.Interfaces;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Constants;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Dtos;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Interfaces;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models;
using Trifted.Points.Data.DbContexts;
using Trifted.Points.Data.Entities;
using Trifted.Points.Data.Entities.WdrbeQuest;
using Trifted.Points.Data.Repositories;

namespace Trifted.Points.Business.Services.WdrbeQuest;

[RepositoryService(Repository = typeof(WdrbeQuestRepository))]
public partial class WdrbeQuestManagerService(
    IQuestEventSubscriptionManagerService questEventSubscriptionManager,
    IUnitOfWork<TriftedPointsDbContext> unitOfWork)
    : Service<WdrbeQuestEntity>(unitOfWork), IWdrbeQuestManagerService
{
    public async Task<CreateWdrbeQuestResponse?> CreateWdrbeQuestAsync(CreateWdrbeQuestDto? request, Guid userId)
    {
        try
        {
            if (request is null)
                throw new ApiValidationException(WdrbeQuestManagerMessages.ModelIsRequired);

            await questEventSubscriptionManager.SubscribeEventTopicToQuestQueue(
                [.. request.Tasks.Select(t => new QuestTaskSubscription
                    {
                        EventTopic = t.EventTopic,
                        UserIdentifier = t.UserIdentifier
                    })]
            );

            return (CreateWdrbeQuestResponse?)await ProcessQuestCreationAsync(request);
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


    private async Task<WdrbeQuestEntity> ProcessQuestCreationAsync(CreateWdrbeQuestDto request)
    {
        var questName = request.QuestName.StripFormat();

        if (!await Repository.IsWdrbeQuestQuestNameUniqueAsync(name: questName))
            throw new ApiValidationException(WdrbeQuestManagerMessages.EventTopicAlreadyCreated);

        int questPoint = request.Tasks.Sum(task => task.PointPerAction * task.MaxAction);
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

    public async Task<GetWdrbeQuestTasksResonse?> WdrbeQuestTaskByIdAsync(string taskId, string questId)
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
                : new GetWdrbeQuestTasksResonse
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
            var questItemCollection = await GetItemCollectionAsync(questId);

            await questEventSubscriptionManager.UnSubscribeEventTopicFromQuestQueue(
                [.. questItemCollection.WdrbeQuestTasks.Select(t => new QuestTaskSubscription
                    {
                        EventTopic = t.EventTopic,
                        UserIdentifier = t.UserIdentifier
                    })]
             );

            Repository.BeginTransaction();

            await Repository.RemoveItemCollectionAsync(questItemCollection);

            await Repository.CommitAsync();

            return new GetWbdrbeQuestResponse
            {
                QuestName = questItemCollection.WdrbeQuest!.Name.ToString(),
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
    public async Task<GetWbdrbeQuestResponse?> UpdateWdrbeQuestByIdAsync(UpdateWdrbeQuestDto request)
    {
        try
        {
            var questItemCollection = await GetItemCollectionAsync(request.QuestId);

            var quest = questItemCollection.WdrbeQuest
                ?? throw new ApiValidationException(WdrbeQuestManagerMessages.QuestNotFound);

            var existingTasks = questItemCollection.WdrbeQuestTasks ?? [];

            var now = CurrentDateTimeAsString;

            var updatedTasks = request.Tasks.Select(t =>
            {
                if (!Guid.TryParse(t.TaskId, out Guid taskId))
                {
                    taskId = Guid.NewGuid();
                }
                var existing = existingTasks.FirstOrDefault(x => x.Id == taskId);

                return new WdrbeQuestTaskEntity
                {
                    Id = taskId == Guid.Empty ? Guid.NewGuid() : taskId,
                    QuestId = quest.Id,
                    Name = t.TaskName,
                    CountryId = t.CountryId,
                    PointPerAction = t.PointPerAction,
                    MaxAction = t.MaxAction,
                    EventTopic = t.EventTopic,
                    UserIdentifier = t.UserIdentifier,
                    Points = t.PointPerAction * t.MaxAction,
                    CreatedOn = existing?.CreatedOn ?? now,
                    LastUpdatedOn = now
                };
            }).ToList();

            var totalPoints = updatedTasks.Sum(x => x.Points);

            var updatedQuest = quest with
            {
                Name = request.QuestName.StripFormat(),
                Description = request.QuestDescription.StripFormat(),
                IsActive = request.IsActive,
                CountryId = request.CountryId,
                Points = totalPoints,
                LastUpdatedOn = now
            };

            var newCollection = new WdrbeQuestRepositoryItemCollection(
                updatedQuest,
                updatedTasks
            );

            var existingSubscriptions = existingTasks
                .Select(x => (x.EventTopic, x.UserIdentifier))
                .ToHashSet();

            var newSubscriptions = updatedTasks
                .Select(x => (x.EventTopic, x.UserIdentifier))
                .ToHashSet();

            var subscriptionsToAdd = newSubscriptions
                .Except(existingSubscriptions)
                .Select(x => new QuestTaskSubscription
                {
                    EventTopic = x.EventTopic,
                    UserIdentifier = x.UserIdentifier
                })
                .ToList();

            var subscriptionsToRemove = existingSubscriptions
                .Except(newSubscriptions)
                .Select(x => new QuestTaskSubscription
                {
                    EventTopic = x.EventTopic,
                    UserIdentifier = x.UserIdentifier
                })
                .ToList();

            Repository.BeginTransaction();

            await Repository.UpsertOrRemoveItemCollectionAsync(
                newCollection,
                questItemCollection
            );

            await Repository.CommitAsync();

            if (subscriptionsToAdd.Count > 0)
                await questEventSubscriptionManager.SubscribeEventTopicToQuestQueue(subscriptionsToAdd);

            if (subscriptionsToRemove.Count > 0)
                await questEventSubscriptionManager.UnSubscribeEventTopicFromQuestQueue(subscriptionsToRemove);

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
            var questItemCollection = await GetItemCollectionAsync(questId);

            return new WdrbeQuestResponse
            {
                QuestId = questItemCollection.WdrbeQuest!.Id,
                QuestName = questItemCollection.WdrbeQuest.Name,
                QuestDescription = questItemCollection.WdrbeQuest.Description,
                Badge = questItemCollection.WdrbeQuest.Badge,
                IsActive = questItemCollection.WdrbeQuest.IsActive,
                TotalPoints = questItemCollection.WdrbeQuest.Points,
                CountryId = questItemCollection.WdrbeQuest.CountryId,
                LastUpdatedOn = questItemCollection.WdrbeQuest.LastUpdatedOn,
                TaskQuest =
                [
                    ..questItemCollection.WdrbeQuestTasks.Select(item => new GetWdrbeQuestTasksResonse
                            {
                                MaxAction = item.MaxAction,
                                TaskId = item.Id,
                                TaskName = item.Name,
                                CountryId = item.CountryId,
                                EventTopic = item.EventTopic,
                                PointPerAction = item.PointPerAction,
                                Points = (item.MaxAction * item.PointPerAction),
                                UserIdentifier = item.UserIdentifier,
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

    public async Task<WdrbeQuestRepositoryItemCollection> GetItemCollectionAsync(string questId)
    {
        if (!Guid.TryParse(questId, out var questGuid))
            throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidQuestId);

        var questItemCollection = await Repository.GetItemCollectionAsync(questGuid)
                                   ?? throw new ApiValidationException(WdrbeQuestManagerMessages.QuestNotFound);

        if (questItemCollection.WdrbeQuest is null)
            throw new ApiValidationException(WdrbeQuestManagerMessages.QuestNotFound);

        if (questItemCollection.WdrbeQuestTasks is null)
            throw new ApiValidationException(WdrbeQuestManagerMessages.TaskNotFound);

        return questItemCollection;
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
                    QuestId = g.Id,
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
    public async Task<List<GetWdrbeQuestTasksResonse>> GetWdrbeQuestTasksAsync()
    {
        try
        {
            var tasks = await Repository.WdrbeQuestTasks.FindWdrbeQuestTasksAsync();
            return
            [
                .. tasks.Select(g => new GetWdrbeQuestTasksResonse
                {
                    TaskId = g.Id,
                    TaskName = g.Name,
                    PointPerAction = g.PointPerAction,
                    CountryId = g.CountryId,
                    EventTopic = g.EventTopic,
                    LastUpdatedOn = g.LastUpdatedOn,
                    MaxAction= g.MaxAction,
                    UserIdentifier = g.UserIdentifier
                })
            ];
        }
        catch (Exception ex) when (ex is not ApiServiceException)
        {
            ex.PrintInConsole(tag: nameof(GetWdrbeQuestTasksAsync));
            throw new ApiServiceException(WdrbeQuestManagerMessages.SystemCouldNotGetQuest);
        }
    }
}