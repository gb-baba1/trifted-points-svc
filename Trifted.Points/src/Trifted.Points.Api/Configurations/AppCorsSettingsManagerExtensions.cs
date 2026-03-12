// Author: Omogbolahan Akinsanya <a.omogbolahan@kanjectbusiness.solutions>
// Copyright (c) 2023 Kanject 2023

using Kanject.Core.Extensions;

namespace Trifted.Points.Api.Configurations;

/// <summary>
///     AppCorsSettingsManagerExtensions
/// </summary>
public static class AppCorsSettingsManagerExtensions
{
    /// <summary>
    ///     UseDefaultAppCors
    /// </summary>
    /// <param name="app"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseDefaultAppCors(this IApplicationBuilder app, IConfiguration configuration)
    {
        string[] whitelistedDomains = configuration["AppSettings:AllowedCorsOrigin"]
            .Split(",", StringSplitOptions.RemoveEmptyEntries)
            .Select(url => url.RemovePostFix("/"))
            .ToArray();

        app.UseCors(builder => builder
                .WithOrigins(whitelistedDomains)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials())
            ;

        return app;
    }
}