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
using Trifted.Points.Data.Entities.WdrbeQuest;
using Trifted.Points.Data.Repositories;

namespace Trifted.Points.Business.Services.WdrbeQuest;

[RepositoryService(Repository = typeof(WdrbeQuestRepository))]
public partial class WdrbeQuestManagerService(
    IServerlessEventHubDataStore serverlessEventHubDataStore,
    IQueueManagerService queueManagerService,
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

            // if (!eventTopics.Parameters.Contains(request.UserIdentifier))
            //     throw new ApiValidationException(
            //         WdrbeQuestManagerMessages.UserIdentifierNotFound.Replace("{{event-topic}}",
            //             request.UserIdentifier));

            var succeeded = await queueManagerService.SubscribeWdrbeQuestQueueToTopicsAsync(topics:
                [..eventTopics.Select(topic => topic.Topic)]);

            if (!succeeded)
                throw new ApiServiceException(WdrbeQuestManagerMessages.SystemCouldNotCreateQuest);

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

    private async Task<WdrbeQuestEntity> ProcessQuestCreationAsync(CreateWdrbeQuestRequest request)
    {
        if (!await Repository.IsWdrbeQuestQuestNameUniqueAsync(
                name: request.QuestName.StripFormat()))
            throw new ApiValidationException(WdrbeQuestManagerMessages.EventTopicAlreadyCreated);

        var totalPoints = request.PointPerAction * request.MaxAction;

        var newQuest = new WdrbeQuestEntity
        {
            Id = Guid.NewGuid(),
            CountryId = request.CountryId,
            UserIdentifier = request.UserIdentifier,
            MaxAction = request.MaxAction,
            PointPerAction = request.PointPerAction,
            Name = request.QuestName.StripFormat(),
            Points = totalPoints,
            CreatedOn = CurrentDateTimeAsString,
            LastUpdatedOn = CurrentDateTimeAsString
        };

        Repository.BeginTransaction();
        await Repository.InsertWithIndexAsync(newQuest);
        await Repository.CommitAsync();

        return newQuest;
    }

    public async Task<GetWdrbeQuestTasksResponse?> WdrbeQuestByIdAsync(string questId)
    {
        try
        {
            if (string.IsNullOrEmpty(questId))
                throw new ApiValidationException(WdrbeQuestManagerMessages.QuestIdIsRequired);

            if (!Guid.TryParse(questId, out var questGUid))
                throw new ApiValidationException(WdrbeQuestManagerMessages.InvalidQuestId);

            var quest = await Repository.FindWdrbeQuestAsync(id: questGUid);
            return quest is null
                ? throw new ApiValidationException(WdrbeQuestManagerMessages.QuestNotFound)
                : new GetWdrbeQuestTasksResponse
                {
                    QuestName = quest.Name,
                    MaxAction = quest.MaxAction,
                    PointPerAction = quest.PointPerAction,
                    CountryId = quest.CountryId,
                    LastUpdatedOn = quest.LastUpdatedOn,
                    Points = quest.Points
                };
        }
        catch (Exception ex) when (ex is not ApiServiceException)
        {
            ex.PrintInConsole(tag: nameof(WdrbeQuestByIdAsync));
            throw new ApiServiceException(WdrbeQuestManagerMessages.SystemCouldNotGetQuest);
        }
    }

    public async Task<List<WdrbeQuestResponse>?> WdrbeQuestsAsync()
    {
        try
        {
            var quests = await Repository.FindWdrbeQuestsUsingGsi1IndexAsync();
            return
            [
                .. quests.Select(g => new WdrbeQuestResponse
                {
                    TotalPoints = g.Points,
                    // WdrbeQuest = g.Select(q => new GetWdrbeQuestResponse
                    // {
                    //     QuestName = q.Name,
                    //     MaxAction = q.MaxAction,
                    //     PointPerAction = q.PointPerAction,
                    //     CountryId = q.CountryId,
                    //     LastUpdatedOn = q.LastUpdatedOn,
                    //     Points = q.Points,
                    // }).FirstOrDefault()
                })
            ];
        }
        catch (Exception ex) when (ex is not ApiServiceException)
        {
            ex.PrintInConsole(tag: nameof(WdrbeQuestByIdAsync));
            throw new ApiServiceException(WdrbeQuestManagerMessages.SystemCouldNotGetQuest);
        }
    }
}