using Amazon.DynamoDBv2.DataModel;
using Kanject.Core.Audit.Interfaces;
using Kanject.Core.Audit.Models;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Abstractions.Interfaces;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;
using Trifted.Points.Data.Entities.Users;
using Trifted.Points.Data.Entities.WdrbeQuest;
using Trifted.Points.Data.Enums;

namespace Trifted.Points.Data.Entities;

/// <summary>
/// Represents an abstract base class for entities in the system, serving as
/// a foundational structure for all entities interacting with DynamoDB.
/// Implements common properties and behaviors for entities such as audit logs,
/// partition keys, sort keys, and versioning for concurrent updates.
/// </summary>
[DynamoDbGsi(Name = "Gsi1Index")]
[DynamoDbGsiAlias<UserPointEntity>(name: "Gsi1Index")]
[DynamoDbGsiAlias<WdrbeQuestEntity>(name: "Gsi1Index")]
[DynamoDbGsi(Name = "Gsi2Index")]
[DynamoDbGsiAlias<UserQuestEntity>(name: "Gsi2Index")]
[DynamoDbGsiAlias<WdrbeQuestEntity>(name: "Gsi2Index")]
public partial record AbstractBaseEntity : IDynamoDbEntity, IAuditEntity
{
    /// <summary>
    /// Represents the partition key used for identifying the primary key in a
    /// DynamoDB table or index. This property typically serves as the hash key
    /// in the table schema, determining how data is distributed and accessed.
    /// </summary>
    [DynamoDBHashKey("pk")]
    public virtual string PartitionKey { get; set; } = string.Empty;

    /// <summary>
    /// Represents the sort key used for structuring and organizing data within a
    /// DynamoDB table or index. This property serves as the range key, enabling
    /// queries and sorting of related items under the same partition key.
    /// </summary>
    [DynamoDBRangeKey("sk")]
    public virtual string SortKey { get; set; } = string.Empty;

    /// <summary>
    /// Represents the unique identifier for an entity in the system.
    /// This property is commonly used to distinguish and locate specific records
    /// across related data models or database tables.
    /// </summary>
    [DynamoDBProperty]
    public virtual Guid Id { get; set; }

    /// <summary>
    /// Represents a collection of audit log entries detailing the history of changes
    /// to the entity. Each entry within the audit log typically includes information
    /// such as the change type, timestamp, and the user or process responsible
    /// for the change.
    /// </summary>
    [DynamoDBProperty]
    public List<AuditDetailsModel> AuditLog { get; set; } = [];

    /// <summary>
    /// Specifies the timestamp when the entity was created. This property is used
    /// to track the creation time of the record, typically formatted as a string.
    /// Useful for auditing and historical tracking purposes.
    /// </summary>
    [DynamoDBProperty]
    public virtual string CreatedOn { get; set; } = string.Empty;

    /// <summary>
    /// Represents a version attribute used for handling optimistic concurrency
    /// control in DynamoDB entities. This property is automatically updated by
    /// DynamoDB when changes are made to the entity, ensuring concurrent updates
    /// do not overwrite each other.
    /// </summary>
    [DynamoDBVersion]
    public int? ConcurrentTokenVersion { get; set; }

    /// <summary>
    /// Specifies the type of entity represented in the system. This property is used
    /// to distinguish the purpose or category of the entity, such as Order, OrderItem,
    /// ShippingRate, and others, as defined in the <c>EntityType</c> enumeration.
    /// </summary>
    [DynamoDBProperty("Et")]
    public virtual EntityType EntityType { get; set; }
}