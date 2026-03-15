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

        if (eventTopics.Count() != questEventTopics.Length)
        {
            var foundEventTopicNames = eventTopics.Select(topic => topic.Topic).ToHashSet();
            var notFoundEventTopics = questEventTopics.Where(topic => !foundEventTopicNames.Contains(topic));
            throw new ApiValidationException(
                WdrbeQuestManagerMessages.EventTopicNotFound.Replace("{{event-topic}}",
                    string.Join(", ", notFoundEventTopics)));
        }

        var topicLookup = eventTopics.ToDictionary(t => t.Topic);

        foreach (var task in questTasks)
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