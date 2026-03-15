using Trifted.Points.Business.Services.QuestEventSubscription.Abstractions.Dtos;
namespace Trifted.Points.Business.Services.QuestEventSubscription.Abstractions.Interfaces;

public interface IQuestEventSubscriptionManager
{
    public Task SubscribeEventTopicToQuestQueue(List<QuestTaskSubscription> questTasks);
    public Task UnSubscribeEventTopicFromQuestQueue(List<QuestTaskSubscription> questTasks);
}