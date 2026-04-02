using Kanject.Core.Api.Aws.Extensions.Vault.ParameterStore;
using Kanject.Core.CacheDb.Provider.DynamoDb.Extensions;
using Kanject.Core.NoSqlDatabase.Provider.DynamoDbV2.Extensions;
using Kanject.Core.Queue.Provider.AwsSqs.Abstractions.Extensions;
using Kanject.Identity.Provider.AwsCognito.Abstractions.Config;
using Kanject.Identity.Provider.AwsCognitoV3;
using Kanject.Identity.Provider.AwsCognitoV3.Extensions;
using Kanject.InstantMessaging.Provider.AwsV2.User;
using Kanject.ServerlessEventHub.Abstractions.Extensions;
using Kanject.ServerlessEventHub.Provider.AwsSns.Abstractions.Config;
using Kanject.ServerlessEventHub.Scheduling.EventBridge.Extensions;
using Trifted.Core.Common.Configuration;
using Trifted.Core.Trifted.Points.Events;
using Trifted.Points.Business.Services.QuestEventSubscription;
using Trifted.Points.Business.Services.QuestEventSubscription.Abstractions.Interfaces;
using Trifted.Points.Business.Services.UserQuest;
using Trifted.Points.Business.Services.UserQuest.Abstractions.Interfaces;
using Trifted.Points.Business.Services.UserStreak;
using Trifted.Points.Business.Services.UserStreak.Abstractions.Interfaces;
using Trifted.Points.Business.Services.WdrbeQuest;
using Trifted.Points.Business.Services.WdrbeQuest.Abstractions.Interfaces;
using Trifted.Points.Common.Constants;
using Trifted.Points.Data.DbContexts;
using Trifted.Points.Data.Repositories;
using Trifted.Points.Tasks.Consumers;
using Trifted.Points.Tasks.RouteConsumers.DefaultQueue;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Trifted.Points.Tasks;

/// <summary>
/// A collection of sample Lambda functions that provide a REST api for doing simple math calculations. 
/// </summary>
public partial class Functions : CloudFunction
{
    private AppSettings? _appSettings;

#if DEBUG
    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    //public Function() : base(CloudFunctionEnvironmentEnum.Production)
     public Functions() : base(CloudFunctionEnvironment.Staging)
    //public Functions() : base(CloudFunctionEnvironment.Development)
    {
    }
#else
    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Functions() : base()
    {
    }
#endif

    /// <summary>
    /// OnStartup
    /// </summary>
    public override void OnStartup(IConfigurationBuilder configurationBuilder)
    {
#if DEBUG
        configurationBuilder.AddAwsSystemManagerParameterStore(fetchSecretPathFromAppSettings: true);
#else
        configurationBuilder.AddAwsSystemManagerParameterStore();
#endif
    }

    /// <summary>
    /// Configure Services
    /// </summary>
    /// <param name="services"></param>
    public override void ConfigureServices(IServiceCollection services)
    {
        var appSettingsConfig = Configuration.GetSection("AppSettings");

        _appSettings = appSettingsConfig.Get<AppSettings>()
                       ?? throw new Exception("AppSettings is required");

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
            options.EnableCreateTableCheck();
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
            options.ApplicationIdentitySchema = "TriftedIdentity";
            options.UserPartitionKey = awsCognitoConfiguration.UserPartitionKey;
        });

        services.AddTriftedPointsSvcEventHubClient(eventHubConfigurationOption: options =>
            {
                var configuration = Configuration.GetSection("EventHubClientConfiguration")
                                        .Get<AwsServerlessEventHubConfiguration>()
                                    ?? throw new Exception("'EventHubClientConfiguration' is required");

                options.AwsAccessKey = _appSettings.AwsAccessKeyId;
                options.AwsSecretKey = _appSettings.AwsAccessSecretKey;
                options.AwsRegionEndpoint = _appSettings.AwsRegion;
                options.ServerlessEventHubSchemaName = configuration.ServerlessEventHubSchemaName;
                options.ServerlessEventLogSchemaName = configuration.ServerlessEventLogSchemaName;
                options.SyncEventTopics = configuration.SyncEventTopics;

#if DEBUG
                options.EnableSetup = configuration.EnableSetup;
                options.EnableForceSync();
#endif
            },
            use: option => option.UseEventBridgeScheduler()
        );

        #region Queue Consumers

        services.AddAwsSqsGlobalQueueConfiguration(options =>
            {
                options.AWSAccessKey = _appSettings.AwsAccessKeyId;
                options.AWSSecretKey = _appSettings.AwsAccessSecretKey;
                options.AWSRegion = _appSettings.AwsRegion;
                options.UseDeadLetterQueue = true;
                options.DlqMessageRetentionPeriod = 14;
                options.CreateQueueIfNotExist = _appSettings.ShouldSetupQueue;
            })
            .AddWdrbeQuestConsumer()
            .AddPointsSvcDefaultQueueEventHubQueueRouter();

        #endregion
        services.AddScoped<IUserQuestManagerService, UserQuestManagerService>();
        services.AddScoped<IWdrbeQuestManagerService, WdrbeQuestManagerService>();
        services.AddScoped<IUserStreakManagerService, UserStreakManagerService>();
        services.AddScoped<IQuestEventSubscriptionManagerService, QuestEventSubscriptionManagerService>();
        services.AddScoped<WdrbeQuestRepository>();
        services.RegisterDynamoDbRepository();
    }

    /// <summary>
    /// Configure
    /// </summary>
    /// <param name="serviceProvider"></param>
    public override void Configure(IServiceProvider serviceProvider)
    {
    }
}