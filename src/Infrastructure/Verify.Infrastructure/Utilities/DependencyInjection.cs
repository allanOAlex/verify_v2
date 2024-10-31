using Quartz;
using Confluent.Kafka;
using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

using Verify.Application.Abstractions.DHT;
using Verify.Application.Abstractions.Interfaces;
using Verify.Application.Abstractions.IRepositories;
using Verify.Application.Abstractions.IServices;
using Verify.Application.Abstractions.MessageQueuing;
using Verify.Infrastructure.Configurations.Caching;
using Verify.Infrastructure.Configurations.Common;
using Verify.Infrastructure.Implementations.Caching;
using Verify.Infrastructure.Implementations.DHT;
using Verify.Infrastructure.Implementations.Interfaces;
using Verify.Infrastructure.Implementations.MessageQueuing.MessageConsumers;
using Verify.Infrastructure.Implementations.MessageQueuing.Transports.Kafka;
using Verify.Infrastructure.Implementations.MessageQueuing.Transports.RabbitMQ;
using Verify.Infrastructure.Implementations.Repositories;
using Verify.Infrastructure.Implementations.Services;
using Refit;
using Verify.Infrastructure.Utilities.DHT.ApiClients;
using Verify.Infrastructure.Implementations.DHT.Jobs;
using Verify.Application.Abstractions.DHT.Jobs;
using DbContext = Verify.Persistence.DataContext.DbContext;

namespace Verify.Infrastructure.Utilities;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheSettings = new CacheSetting();
        configuration.GetSection("CacheSettings").Bind(cacheSettings);
        services.AddSingleton(cacheSettings);
        CacheKeyGenerator.Configure(cacheSettings);

        var paginationSetting = new PaginationSetting();
        configuration.GetSection("PaginationSetting").Bind(paginationSetting);
        services.AddSingleton(paginationSetting);

        var connString = configuration.GetConnectionString("KHS");
        services.AddDbContext<DbContext>(options => options.UseSqlServer(connString!));

        var refitSettings = new RefitSettings(); // Customize if needed
        services.AddSingleton<IApiClientFactory>(new ApiClientFactory(refitSettings));

        // Kafka ProducerConfig
        services.AddScoped<IMessageProducer, KafkaMessageProducer>();
        services.AddSingleton(new ProducerConfig { BootstrapServers = "localhost:9092" });

        // Kafka consumer configuration
        services.AddSingleton<IMessageConsumer, KafkaMessageConsumer>();
        services.AddSingleton(new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "my-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        });

        // MassTransit and RabbitMQ configuration
        services.AddScoped<IMessageProducer, RabbitMqBusMessageProducer>();
        services.AddScoped<IMessageProducer, RabbitMqBusControlMessageProducer>();
        // RabbitMQ consumer
        services.AddSingleton<IMessageConsumer, RabbitMqBusControlMessageConsumer>();
        services.AddMassTransit(x =>
        {
            x.AddConsumer<AccountMessageConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                //cfg.Host("host.docker.internal", "/", h =>
                //{
                //    h.Username("guest");
                //    h.Password("guest");

                //    h.UseSsl(s =>
                //    {
                //        s.Protocol = SslProtocols.Tls12; // Ensure correct SSL protocol
                //        s.ServerName = "localhost"; // Match the server name to certificate's CN or subject alternative name
                //        s.CertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true; // Use this for dev; otherwise, use proper validation.
                //    });
                //});

                // Connect the consumer to a receive endpoint
                cfg.ReceiveEndpoint("my-queue", e =>
                {
                    e.ConfigureConsumer<AccountMessageConsumer>(context);
                });

            });
        });

        services.Configure<QuartzOptions>(configuration.GetSection("Quartz"));

        // if you are using persistent job store, you might want to alter some options
        services.Configure<QuartzOptions>(options =>
        {
            options.Scheduling.IgnoreDuplicates = true; // default: false
            options.Scheduling.OverWriteExistingData = true; // default: true
        });

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("DHTMaintenanceJob");
            q.AddJob<DhtMaintenanceJob>(opts => opts
                .WithIdentity(jobKey));

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("DHTMaintenanceJob-trigger")
                //.WithCronSchedule("0 0 * * * ?")); // Every hour
                .WithSimpleSchedule(x => x
                    .WithInterval(TimeSpan.FromMinutes(5))
                    .RepeatForever())
            );

        });

        //Add the Quartz hosted service
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        //services.AddRefitClient<IApiClient>()
        //.ConfigureHttpClient(client =>
        //{
        //    client.BaseAddress = new Uri("https://api.example.com");
        //    client.Timeout = TimeSpan.FromSeconds(30);
        //}).AddPolicyHandler(DHTUtilities.GetRetryPolicy());

        switch (cacheSettings.CacheType)
        {
            case { } type when type.Equals("redis", StringComparison.OrdinalIgnoreCase):

                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(cacheSettings.Redis!.Configuration!));

                // Register the IDatabase from the connection multiplexer
                services.AddScoped(sp =>
                {
                    var connectionMultiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
                    return connectionMultiplexer.GetDatabase();
                });

                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = cacheSettings.Redis!.Configuration;
                    options.InstanceName = cacheSettings.Redis.InstanceName;
                });
                services.AddSingleton<ICacheService, RedisMultiplexerCacheService>();
                services.AddSingleton<ICacheService, RedisCacheService>();
                break;

            case { } type when type.Equals("azure", StringComparison.OrdinalIgnoreCase):
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = cacheSettings.Azure!.ConnectionString;
                });
                services.AddSingleton<ICacheService, AzureCacheService>();
                break;

            case { } type when type.Equals("aws", StringComparison.OrdinalIgnoreCase):
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = cacheSettings.Aws!.Endpoint;
                });
                services.AddSingleton<ICacheService, ElastiCacheService>();
                break;

            default:
                services.AddMemoryCache();
                services.AddSingleton<ICacheService, InMemoryCacheService>();
                break;
        }

        services.AddScoped<IServiceManager, ServiceManager>();
        services.AddScoped<ILogService, LogService>();
        services.AddScoped<IDhtService, DhtService>();
        services.AddScoped<IDhtRedisService, DhtRedisService>();
        services.AddScoped<IHashingService, HashingService>();
        services.AddScoped<INodeManagementService, NodeManagementService>();
        services.AddScoped<IDhtMaintenanceJob, DhtMaintenanceJob>();
        services.AddScoped<IAddNodeToPeersJob, AddNodeToPeersJob>();
        //services.AddHostedService<CentralNodeInitializer>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddTransient(typeof(IBaseRepository<>), typeof(BaseRepository<>));
        services.AddScoped<ILogRepository, LogRepository>();

        return services;
    }
}

