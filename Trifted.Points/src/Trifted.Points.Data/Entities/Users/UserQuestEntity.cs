using Amazon.DynamoDBv2.DataModel;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;
using Trifted.Points.Data.Enums;

namespace Trifted.Points.Data.Entities.Users;

public record UserQuestEntity : AbstractBaseEntity
{
    [KeyTemplate("TriftedUserEntity#{UserId}")]
    [DynamoDBHashKey("pk")]
    public override string PartitionKey { get; set; } = string.Empty;

    [KeyTemplate("Quests#{QuestId}")]
    [DynamoDBRangeKey("sk")]
    public override string SortKey { get; set; } = string.Empty;

    [KeyTemplate("UserQuests")]
    [DynamoDBGlobalSecondaryIndexHashKey]
    public override string Gsi1Pk { get; set; } = string.Empty;

    [KeyTemplate("Quests#{QuestId}#{Points}#{Id}")]
    [DynamoDBGlobalSecondaryIndexRangeKey]
    public override string Gsi1Sk { get; set; } = string.Empty;

    [DynamoDBProperty] public Guid UserId { get; set; }
    [DynamoDBProperty] public Guid QuestId { get; set; }
    [DynamoDBProperty] public int Points { get; set; }

    [DynamoDBProperty("Et")] public override EntityType EntityType { get; set; } = EntityType.UserQuest;
}