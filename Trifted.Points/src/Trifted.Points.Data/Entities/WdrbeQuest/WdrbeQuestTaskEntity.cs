using Amazon.DynamoDBv2.DataModel;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes.Indexes;
using Trifted.Points.Data.Enums;

namespace Trifted.Points.Data.Entities.WdrbeQuest;

public record WdrbeQuestTaskEntity : AbstractBaseEntity
{
    [KeyTemplate("WdrbeQuest#{QuestId}")]
    [DynamoDBHashKey("pk")]
    public override string PartitionKey { get; set; } = string.Empty;

    [KeyTemplate("Tasks#{Id}")]
    [DynamoDBRangeKey("sk")]
    public override string SortKey { get; set; } = string.Empty;

    /// <summary>
    /// Represents the Partition Key (Hash Key) for the "Gsi1Index" Global Secondary Index (GSI)
    /// in DynamoDB. The value of this property is determined based on the defined template
    /// in the associated KeyTemplate attribute.
    /// </summary>
    /// <remarks>
    /// This property is designed to support advanced querying capabilities by serving as the
    /// hash key in the Global Secondary Index (GSI). It is primarily used for efficient retrieval
    /// of entities, such as "WdrbeQuests", grouped under the same GSI partition. The usage of
    /// a partitioned and indexed structure in DynamoDB enables scalability and optimized query
    /// performance for workloads with non-primary key access patterns.
    /// </remarks>
    [KeyTemplate("WdrbeQuests")]
    [DynamoDBGlobalSecondaryIndexHashKey]
    public override string Gsi1Pk { get; set; } = string.Empty;

    /// <summary>
    /// Represents the Sort Key (Range Key) for the "Gsi1Index" Global Secondary Index (GSI)
    /// in DynamoDB. The value of this property is dynamically constructed based on the template
    /// defined in the associated KeyTemplate attribute.
    /// </summary>
    /// <remarks>
    /// This property plays a crucial role in organizing and enabling precise queries within
    /// the Global Secondary Index (GSI). It functions as the range key, supporting advanced
    /// filtering and sorting capabilities for entities such as "WdrbeQuests" within the same
    /// partition. By leveraging this property in conjunction with the Partition Key, DynamoDB
    /// achieves efficient and scalable query performance for varied access patterns.
    /// </remarks>
    [KeyTemplate("WdrbeQuestInfo#{CountryId}#{CreatedOn}#{Id}")]
    [DynamoDBGlobalSecondaryIndexRangeKey]
    public override string Gsi1Sk { get; set; } = string.Empty;

    [CompositeUnique("UniqueQuest")]
    [Collection(name: "QuestEventTopic")]
    [DynamoDBProperty]
    public string EventTopic { get; set; } = string.Empty;

    [DynamoDBProperty] public Guid QuestId { get; set; }

    [DynamoDBProperty] public Guid? CountryId { get; set; }

    [DynamoDBProperty] public string UserIdentifier { get; set; } = string.Empty;

    [DynamoDBProperty("Et")] public override EntityType EntityType { get; set; } = EntityType.WdrbeQuestTask;
}