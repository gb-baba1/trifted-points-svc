using Kanject.Core.NoSqlDatabase.Provider.DynamoDb.Abstractions.Interfaces;
using Trifted.Points.Data.Entities.Users;

namespace Trifted.Points.Business.Services.UserStreak.Abstractions.Interfaces;

public interface IUserStreakManagerService : IService<UserProfileEntity>, IUserStreakManager;