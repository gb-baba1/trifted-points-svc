using Amazon.DynamoDBv2.DataModel;

namespace Trifted.Points.Business.Services.UserStreak.Abstractions.Dtos
{
    public class UserStreakResponse
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public DateTime Date { get; set; }
        public bool IsBroken { get; set; }
    }
}
