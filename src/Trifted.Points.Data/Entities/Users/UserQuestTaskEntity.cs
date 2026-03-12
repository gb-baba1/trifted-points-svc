using Amazon.DynamoDBv2.DataModel;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;
using Trifted.Points.Data.Enums;

namespace Trifted.Points.Data.Entities.Users
{
    public record UserQuestTaskEntity : AbstractBaseEntity
    {
        [KeyTemplate("TriftedUserEntity#{UserId}")]
        [DynamoDBHashKey("pk")]
        public override string PartitionKey { get; set; } = string.Empty;

        [KeyTemplate("QuestTasks#{QuestId}#{TaskId}")]
        [DynamoDBRangeKey("sk")]
        public override string SortKey { get; set; } = string.Empty;

        [KeyTemplate("UserQuestTasks")]
        [DynamoDBGlobalSecondaryIndexHashKey]
        public override string Gsi1Pk { get; set; } = string.Empty;

        [KeyTemplate("QuestTasks#{QuestId}#{TaskId}#{Points}#{Id}")]
        [DynamoDBGlobalSecondaryIndexRangeKey]
        public override string Gsi1Sk { get; set; } = string.Empty;

        [DynamoDBProperty] public Guid UserId { get; set; }
        [DynamoDBProperty] public Guid QuestId { get; set; }
        [DynamoDBProperty] public Guid TaskId { get; set; }
        [DynamoDBProperty] public int Points { get; set; }
        [DynamoDBProperty] public bool IsCompleted { get; set; }
        [DynamoDBProperty] public DateTime? CompletedOn { get; set; }

        [DynamoDBProperty("Et")] public override EntityType EntityType { get; set; } = EntityType.UserQuestTask;
    }
}
