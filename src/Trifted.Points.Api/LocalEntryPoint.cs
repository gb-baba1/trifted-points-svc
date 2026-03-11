using Trifted.Points.Api.Configurations;
using Trifted.Points.Api.Constants;
using Kanject.Core.Api.Aws.Extensions.Vault.ParameterStore;
using Kanject.Core.Extensions;

namespace Trifted.Points.Api;

/// <summary>
///     The Main function can be used to run the ASP.NET Core application locally using the Kestrel webserver.
/// </summary>
public class LocalEntryPoint
{
    /// <summary>
    ///     Main
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    /// <summary>
    ///     CreateHostBuilder
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                var envName = Environment.GetEnvironmentVariable(AppConstants.ASPNETCORE_LAMBDA_ENVIRONMENT);

                if (string.IsNullOrEmpty(envName))
                    envName = context.HostingEnvironment.EnvironmentName;

                config
                    .AddDefaultAppSettings(envName)
                    .AddSubEnvironment(context);

                config.AddAwsSystemManagerParameterStore(fetchSecretPathFromAppSettings: true);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>()
                    .ConfigureSubEnvironment();
            });
    }
}