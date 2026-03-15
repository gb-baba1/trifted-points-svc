using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Abstractions.Interfaces;
using Trifted.Points.Data.Entities.Users;

namespace Trifted.Points.Business.Services.UserQuest.Abstractions.Interfaces;

public interface IUserQuestManagerService : IService<UserProfileEntity>, IUserQuestManager;