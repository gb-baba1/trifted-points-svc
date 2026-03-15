using Amazon.Lambda.SQSEvents;
using Amazon.SQS.Model;
using Kanject.Core.Api.Abstractions.Exceptions;
using Kanject.Core.Queue.Provider.AwsSqs.Annotations.Attributes;
using System.Text.Json;
using Trifted.Points.Business.Services.UserQuest.Abstractions.Interfaces;
using Trifted.Points.Data;
using Trifted.Points.Data.Repositories;
using static Trifted.Points.Data.Repositories.WdrbeQuestRepositoryItemCollectionType;

namespace Trifted.Points.Tasks.Consumers;

[QueueConsumer]
[QueueConsumerDependency(typeof(WdrbeQuestRepository))]
[QueueConsumerDependency(typeof(IUserQuestManagerService))]
public partial class WdrbeQuestConsumer
{
    protected override async Task ConsumeAsync(List<Message> messages)
    {
        var topics = messages.Select(message => message.MessageAttributes["EventTopic"].StringValue).ToArray();
        var questTasks = await WdrbeQuestRepository.WdrbeQuestTasks
            .FindWdrbeQuestTasksByQuestEventTopicsAsync(topics);

        var questTasksLookup = questTasks.ToLookup(task => task.EventTopic);

        var questLookup = (await WdrbeQuestRepository.GetItemCollectionAsync(
                ids: [.. questTasks.Select(task => task.QuestId)],
                includes:
                [
                    WdrbeQuest,
                    WdrbeQuestTasks
                ]))
            .ToLookup(quest => quest.WdrbeQuest!.Id);

        foreach (var messageContext in messages)
        {
            try
            {
                var eventTopic = messageContext.MessageAttributes["EventTopic"].StringValue;

                if (string.IsNullOrWhiteSpace(eventTopic))
                {
                    "Unable to find event topic in message attributes".PrintInConsole();
                    continue;
                }

                var parsedMessage = JsonSerializer.Deserialize<JsonElement>(messageContext.Body);

                var relatedQuests = questTasksLookup[eventTopic];

                foreach (var quest in relatedQuests)
                {
                    var userIdentifier = parsedMessage.GetProperty(quest.UserIdentifier).GetString();

                    if (!Guid.TryParse(userIdentifier, out var userId))
                    {
                        "Unable to parse user identifier from message body".PrintInConsole();
                        messageContext.PrintInConsole(tag: nameof(ConsumeAsync));
                        throw new ApiValidationException("Unable to parse user identifier from message body");
                    }

                    var response = await UserQuestManagerService.ProcessUserPointAsync(userId: userId,
                        questId: quest.QuestId, taskId: quest.Id);

                    response.PrintInConsole();
                }
            }
            catch (Exception ex)
            {
                ex.PrintInConsole();
                Response.BatchItemFailures.Add(new SQSBatchResponse.BatchItemFailure
                { ItemIdentifier = messageContext.MessageId });
            }
        }
    }
}