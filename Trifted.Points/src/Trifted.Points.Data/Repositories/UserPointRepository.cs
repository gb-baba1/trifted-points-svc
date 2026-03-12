using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Annotations.Attributes;
using Trifted.Points.Data.DbContexts;
using Trifted.Points.Data.Entities.Users;

namespace Trifted.Points.Data.Repositories;

[Repository(Entity = typeof(UserQuestEntity), Version = 2)]
[DbContext(typeof(TriftedPointsDbContext))]
[EntityRepository<UserPointEntity>]
public partial class UserPointRepository
{
}