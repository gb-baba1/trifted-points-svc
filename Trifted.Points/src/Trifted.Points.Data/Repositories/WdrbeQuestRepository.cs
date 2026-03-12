using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;
using Trifted.Points.Data.DbContexts;
using Trifted.Points.Data.Entities.WdrbeQuest;

namespace Trifted.Points.Data.Repositories;

[Repository(Entity = typeof(WdrbeQuestEntity), Version = 2)]
[DbContext(typeof(TriftedPointsDbContext))]
[EntityRepository<WdrbeQuestTaskEntity>]
public partial class WdrbeQuestRepository
{

}