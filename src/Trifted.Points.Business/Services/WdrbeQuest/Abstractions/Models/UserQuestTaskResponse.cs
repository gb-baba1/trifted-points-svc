namespace Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Models
{
    public class UserQuestTaskResponse
    {
        public string TaskName { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public int PointsEarned { get; set; }
        public decimal CompletionPercentage { get; set; }
        public bool IsCompleted { get; set; }
    }
}
