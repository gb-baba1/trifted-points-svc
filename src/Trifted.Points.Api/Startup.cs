using System.Reflection;
using System.Text.Json.Serialization;
using Kanject.Core.CacheDb.Provider.DynamoDb.Extensions;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDbV2.Extensions;
using Kanject.Core.UtilityServices.Extensions;
using Kanject.Identity.Provider.AwsCognito.Abstractions.Config;
using Kanject.Identity.Provider.AwsCognitoV3.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using Trifted.Core.Common.Configuration;
using Trifted.Points.Api.Components.Filters;
using Trifted.Points.Api.Configurations;
using Trifted.Points.Common.Constants;
using Trifted.Points.Data.DbContexts;

namespace Trifted.Points.Api;

/// <summary>
///     Startup
/// </summary>
/// <remarks>
/// Default constructor
/// </remarks>
/// <param name="configuration"></param>
public class Startup(IConfiguration configuration)
{
    private IConfiguration Configuration { get; } = configuration;
    private bool _shouldSetupQueue;
    private AppSettings? _appSettings;

    /// <summary>
    ///     This method gets called by the runtime. Use this method to add services to the container
    /// </summary>
    /// <param name="services"></param>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCors();
        services.AddEndpointsApiExplorer();


        var appSettingsConfig = Configuration.GetSection("AppSettings");

        _appSettings = appSettingsConfig.Get<AppSettings>()
                       ?? throw new Exception("AppSettings is required");

        #region SwaggerGen configuration

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Trifted.Points API", Version = "v1" });

            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename),
                includeControllerXmlComments: true);
            options.DescribeAllParametersInCamelCase();

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = JwtBearerDefaults.AuthenticationScheme
            });
            options.AddSecurityDefinition("AppSecret", new OpenApiSecurityScheme
            {
                Description = @"Example: '12345abcdef'",
                Name = "appkey",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            options.SchemaFilter<EnumSchemaFilter>();
        });

        #endregion

        services.AddDynamoCacheDb(options =>
        {
            options.Namespace = _appSettings.DatabaseNamespace;
            options.AccessKey = _appSettings.AwsAccessKeyId;
            options.SecretKey = _appSettings.AwsAccessSecretKey;
            options.AwsRegion = _appSettings.AwsRegion;
            options.KeyPrefix = AppConstants.CacheKeyPrefix;
            options.EnableHighStorageVolume();

            if (_appSettings.EnableHighAvailabilityCache)
                options.MaximumReadReplicas = 2;

#if DEBUG
            options.EnableDebugLogging();
#endif
        });

        services.AddDbContext<TriftedPointsDbContext>(options =>
        {
            options.TableName = AppConstants.TableSchemaName;
            options.Namespace = _appSettings.DatabaseNamespace;
            options.SecretKey = _appSettings.AwsAccessSecretKey;
            options.AccessKey = _appSettings.AwsAccessKeyId;
            options.AwsRegion = _appSettings.AwsRegion;
            options.EnableQueryTranspilation = true;

#if DEBUG
            options.EnableDbMigration();
            options.EnableDebugLogging();
#endif
        });

        services.AddCognitoIdentityProviderSettings(options =>
        {
            var awsCognitoConfiguration =
                Configuration.GetSection("CognitoIdentityProviderSettings").Get<AwsCognitoConfiguration>()
                ?? throw new Exception("'CognitoIdentityProviderSettings' is required");

            options.AccessKeyId = _appSettings.AwsAccessKeyId;
            options.AccessSecretKey = _appSettings.AwsAccessSecretKey;
            options.Region = awsCognitoConfiguration.Region;
            options.ClientId = awsCognitoConfiguration.ClientId;
            options.UserPoolId = awsCognitoConfiguration.UserPoolId;
            options.ServiceClientId = awsCognitoConfiguration.ServiceClientId;
            options.ServiceClientSecret = awsCognitoConfiguration.ServiceClientSecret;
            options.AuthenticationUrl = awsCognitoConfiguration.AuthenticationUrl;
            options.SyncPermissions = awsCognitoConfiguration.SyncPermissions;

            options.IdentityStoreNamespace = _appSettings.DatabaseNamespace;
            options.ApplicationIdentitySchema = awsCognitoConfiguration.ApplicationIdentitySchema;
            options.UserPartitionKey = awsCognitoConfiguration.UserPartitionKey;

            // services.AddAuthorizationServerWithPermissions(option =>
            // {
            //     option.RegisterDefaultClientPolicies();
            //     option.RegisterDefaultUserGroupPolicies();
            // });
        });


        services.AddBusinessServices(); //Registers all business services dependencies

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
    }

    /// <summary>
    ///     This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    /// </summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseStaticFiles();

        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        #region Swagger Configuration

        app.UseSwaggerUI(options =>
        {
#if DEBUG
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
#else
            options.SwaggerEndpoint(env.IsDevelopment()
                    ? "/swagger/v1/swagger.json"
                    : "/Stage/swagger/v1/swagger.json",
                "v1");
#endif

            options.RoutePrefix = string.Empty;
        });

        app.UseSwagger();
        app.UseSwaggerUI();

        #endregion

        app.UseDefaultAppCors(
            Configuration); //Configure default app CORS using whitelisted domain names in AppSettings.json file
        app.UseAppUtilityService(env); //Registers Kanject Core dependencies

        app.UseRouting();

        app.UseAuthentication();

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}