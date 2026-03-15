namespace Trifted.Points.Business.Services.QuestEventSubscription.Abstractions.Dtos
{
    public class QuestTaskSubscription
    {
        public string EventTopic { get; set; } = string.Empty;
        public string UserIdentifier { get; set; } = string.Empty;
    }
}
