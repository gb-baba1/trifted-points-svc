using Amazon.DynamoDBv2.DataModel;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;

namespace Trifted.Points.Data.Entities.Users;

public record UserProfileEntity : AbstractBaseEntity
{
    [KeyTemplate("TriftedUserEntity#{UserId}")]
    [DynamoDBHashKey("pk")]
    public override string PartitionKey { get; set; } = string.Empty;

    [KeyTemplate("Info")]
    [DynamoDBRangeKey("sk")]
    public override string SortKey { get; set; } = string.Empty;

    [DynamoDBProperty] public Guid UserId { get; set; }
}