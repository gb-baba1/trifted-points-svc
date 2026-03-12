using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Abstractions.Interfaces;
using Trifted.Points.Data.Entities.WdrbeQuest;

namespace Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Interfaces;

public interface IWdrbeQuestManagerService : IService<WdrbeQuestEntity>, IWdrbeQuestManager
{
}