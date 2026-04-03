using Trifted.Points.Data.Enums;

namespace Trifted.Points.Business.Services.UserQuest.Abstractions.Dtos
{
    public class UserPointResponse
    {
        public int PointsEarned { get; set; }
        public Badge Badge { get; set; }
        public List<UserQuestResponse> Quests { get; set; } = [];
    }
}