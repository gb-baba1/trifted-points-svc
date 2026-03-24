using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Dtos;
using Trifted.Points.Data.Enums;

namespace Trifted.Points.Business.Services.UserQuest.Abstractions.Dtos
{
    public class UserQuestResponse
    {
        public string QuestName { get; set; } = string.Empty;
        public string QuestDescription { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public int PointsEarned { get; set; }
        public int CompletionPercentage { get; set; }
        public Badge Badge { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsActive { get; set; }
        public List<UserQuestTaskResponse>? QuestTasks { get; set; }
    }
}
