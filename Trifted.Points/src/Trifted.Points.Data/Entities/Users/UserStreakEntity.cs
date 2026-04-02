using Amazon.DynamoDBv2.DataModel;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;
using Trifted.Points.Data.Enums;

namespace Trifted.Points.Data.Entities.Users;

public record UserStreakEntity : AbstractBaseEntity
{
    [KeyTemplate("TriftedUserEntity#{UserId}")]
    [DynamoDBHashKey("pk")]
    public override string PartitionKey { get; set; } = string.Empty;

    [KeyTemplate("UserStreak#{Date}")]
    [DynamoDBRangeKey("sk")]
    public override string SortKey { get; set; } = string.Empty;

    [KeyTemplate("UserStreaks")]
    [DynamoDBGlobalSecondaryIndexHashKey]
    public override string Gsi1Pk { get; set; } = string.Empty;

    [KeyTemplate("UserStreak#{UserId}#{Date}")]
    [DynamoDBGlobalSecondaryIndexRangeKey]
    public override string Gsi1Sk { get; set; } = string.Empty;

    [DynamoDBProperty]
    public Guid UserId { get; set; }

    [DynamoDBProperty]
    public DateTime Date { get; set; }

    [DynamoDBProperty]
    public int CurrentStreak { get; set; }

    [DynamoDBProperty]
    public int LongestStreak { get; set; }

    [DynamoDBProperty]
    public bool IsBroken { get; set; }

    [DynamoDBProperty]
    public DateTime? BrokenOn { get; set; }

    [DynamoDBProperty("Et")]
    public override EntityType EntityType { get; set; } = EntityType.UserStreak;
}