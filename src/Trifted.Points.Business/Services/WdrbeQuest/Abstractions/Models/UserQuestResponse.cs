namespace Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models
{
    public class UserQuestResponse
    {
        public string QuestName { get; set; } = string.Empty;
        public string QuestDescription { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public int PointsEarned { get; set; }
        public int CompletionPercentage { get; set; }
        public bool IsCompleted { get; set; }
        public List<UserQuestTaskResponse>? QuestTasks { get; set; }
    }
}
