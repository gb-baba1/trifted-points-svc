using Amazon.DynamoDBv2.DataModel;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;
using Trifted.Points.Data.Enums;

namespace Trifted.Points.Data.Entities.Users;

public record UserPointEntity : AbstractBaseEntity
{
    [KeyTemplate("TriftedUserEntity#{UserId}")]
    [DynamoDBHashKey("pk")]
    public override string PartitionKey { get; set; } = string.Empty;

    [KeyTemplate("UserProfile#Users#{UserId}")]
    [DynamoDBRangeKey("sk")]
    public override string SortKey { get; set; } = string.Empty;

    [KeyTemplate("QuestPosts")]
    [DynamoDBGlobalSecondaryIndexHashKey]
    public override string Gsi1Pk { get; set; } = string.Empty;

    [KeyTemplate("Points#{Points}#{UserId}#{Id}")]
    [DynamoDBGlobalSecondaryIndexRangeKey]
    public override string Gsi1Sk { get; set; } = string.Empty;

    [DynamoDBProperty] public Guid UserId { get; set; }
    [DynamoDBProperty] public int Points { get; set; }

    [DynamoDBProperty("Et")] public override EntityType EntityType { get; set; } = EntityType.UserPoint;
}