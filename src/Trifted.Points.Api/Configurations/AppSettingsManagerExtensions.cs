//  Author: Omogbolahan Akinsanya a.omogbolahan@kanjectbusiness.solutions
//  Copyright (c) 2022, Kanject 2015

namespace Trifted.Points.Api.Configurations;

/// <summary>
///     AppSettingsManagerExtensions
/// </summary>
public static class AppSettingsManagerExtensions
{
    /// <summary>
    ///     AddDefaultAppSettings
    /// </summary>
    /// <param name="configurationBuilder"></param>
    /// <param name="environmentName"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddDefaultAppSettings(this IConfigurationBuilder configurationBuilder,
        string environmentName)
    {
        configurationBuilder
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile($"appsettings.{environmentName}.json", true, true)
            .AddEnvironmentVariables();

        return configurationBuilder;
    }
}