using Kanject.Core.Api.Abstractions.Exceptions;
using Kanject.Core.Queue.Abstractions.Interfaces;
using Kanject.ServerlessEventHub.Provider.AwsSns.Abstractions.DataStore;

using Trifted.Core.Trifted.Points.Queues;
using Trifted.Points.Business.Services.QuestEventSubscription.Abstractions.Interfaces;
using Trifted.Points.Business.Services.QuestEventSubscription.Abstractions.Dtos;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Constants;

namespace Trifted.Points.Business.Services.QuestEventSubscription;

public partial class QuestEventSubscriptionManagerService(
    IServerlessEventHubDataStore serverlessEventHubDataStore,
    IQueueManagerService queueManagerService) : IQuestEventSubscriptionManagerService
{
    public async Task SubscribeEventTopicToQuestQueue(List<QuestTaskSubscription> questTasks)
    {
        string[] questEventTopics = [.. questTasks.Select(task => task.EventTopic)];

        var eventTopics = (await serverlessEventHubDataStore.GetServiceTopicsByNameAsync(topic:
                              questEventTopics))
                          ?? throw new ApiValidationException(WdrbeQuestManagerMessages.EventTopicNotFound);

        var foundEventTopicNames = eventTopics
            .Select(x => x.Topic.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var notFoundEventTopics = questEventTopics
            .Select(x => x.Trim())
            .Where(topic => !foundEventTopicNames.Contains(topic))
            .ToList();

        if (notFoundEventTopics.Count > 0)
        {
            throw new ApiValidationException(
                WdrbeQuestManagerMessages.EventTopicNotFound.Replace(
                    "{{event-topic}}",
                    string.Join(", ", notFoundEventTopics)
                )
            );
        }

        var topicLookup = eventTopics.ToLookup(t => t.Topic, StringComparer.OrdinalIgnoreCase);

        foreach (var task in questTasks)
        {

            var topics = topicLookup[task.EventTopic];

            if (!topics.Any())
            {
                throw new ApiValidationException(
                    WdrbeQuestManagerMessages.EventTopicNotFound
                        .Replace("{{event-topic}}", task.EventTopic));
            }
            if (!topics.Any(t => t.Parameters.Contains(task.UserIdentifier)))
            {
                throw new ApiValidationException(
                    WdrbeQuestManagerMessages.UserIdentifierNotFound
                        .Replace("{{event-topic}}", task.EventTopic));
            }
        }

        var succeeded = await queueManagerService.SubscribeWdrbeQuestQueueToTopicsAsync(topics:
            [.. eventTopics.Select(topic => topic.Topic)]);

        if (!succeeded)
            throw new ApiServiceException(WdrbeQuestManagerMessages.SystemCouldNotCreateQuest);
    }

    public async Task UnSubscribeEventTopicFromQuestQueue(List<QuestTaskSubscription> questTasks)
    {
        string[] questEventTopics = [.. questTasks.Select(task => task.EventTopic)];

        var eventTopics = (await serverlessEventHubDataStore.GetServiceTopicsByNameAsync(topic:
                              questEventTopics))
                          ?? throw new ApiValidationException(WdrbeQuestManagerMessages.EventTopicNotFound);



        var succeeded = await queueManagerService.UnsubscribeWdrbeQuestQueueFromTopicsAsync(topics:
            [.. eventTopics.Select(topic => topic.Topic)]);

        if (!succeeded)
            throw new ApiServiceException(WdrbeQuestManagerMessages.SystemCouldNoUnsubscribeQuestTopic);
    }
}