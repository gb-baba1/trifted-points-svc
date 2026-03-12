using Amazon.DynamoDBv2;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Abstractions.DataContext.EntityBuilder;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;
using Trifted.Points.Data.Entities;
using Trifted.Points.Data.Entities.Users;
using Trifted.Points.Data.Entities.WdrbeQuest;

namespace Trifted.Points.Data.DbContexts;

[DbContext]
[EnableRecordLifeTimeSupport]
public partial class TriftedPointsDbContext
{
    protected override void OnModelCreating(EntityModelBuilder builder)
    {
        builder.HasPartitionKey(name: "pk", datatype: ScalarAttributeType.S);
        builder.HasSortKey(name: "sk", datatype: ScalarAttributeType.S);

        builder.HasGlobalSecondaryIndexes(indexOptions: option =>
        {
            option.AddGsi1Index(hashKeyType: ScalarAttributeType.S, rangeKeyType: ScalarAttributeType.S);
            option.AddGsi2Index(hashKeyType: ScalarAttributeType.S, rangeKeyType: ScalarAttributeType.S);
        });

        builder.MapEntity<UserProfileEntity>();
        builder.MapEntity<UserPointEntity>();
        builder.MapEntity<UserQuestEntity>();

        builder.MapEntity<WdrbeQuestEntity>();
        builder.MapEntity<WdrbeQuestTaskEntity>();
    }
}