//  Author: Omogbolahan Akinsanya a.omogbolahan@kanjectbusiness.solutions
//  Copyright (c) 2022, Kanject 2015

using Trifted.Points.Business.Services.QuestEventSubscription;
using Trifted.Points.Business.Services.QuestEventSubscription.Abstractions.Interfaces;
using Trifted.Points.Business.Services.UserQuest;
using Trifted.Points.Business.Services.UserQuest.Abstractions.Interfaces;
using Trifted.Points.Business.Services.WdrbeQuest;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Interfaces;
using Trifted.Points.Data.Repositories;

namespace Trifted.Points.Api.Configurations;

/// <summary>
///     Register's default LegalMate App Services
/// </summary>
public static class AppServicesManagerExtensions
{
    /// <summary>
    ///     AddBusinessServices
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {

        services.AddScoped<IWdrbeQuestManagerService, WdrbeQuestManagerService>();
        services.AddScoped<IUserQuestManagerService, UserQuestManagerService>();
        services.AddScoped<IQuestEventSubscriptionManagerService, QuestEventSubscriptionManagerService>();
        services.AddScoped<UserQuestRepository>();
        return services;
    }
}