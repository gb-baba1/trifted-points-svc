//  Author: Omogbolahan Akinsanya a.omogbolahan@kanjectbusiness.solutions
//  Copyright (c) 2022, Kanject 2015

using Trifted.Points.Business.Services.WdrbeQuest;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Interfaces;

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
        return services;
    }
}