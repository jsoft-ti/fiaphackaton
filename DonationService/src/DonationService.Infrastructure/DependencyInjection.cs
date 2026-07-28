using DonationService.Application.Common.Interfaces;
using DonationService.Contracts.Events.V1;
using DonationService.Domain.Repositories;
using DonationService.Infrastructure.ExternalServices;
using DonationService.Infrastructure.Messaging;
using DonationService.Infrastructure.Persistence;
using DonationService.Infrastructure.Persistence.Mongo;
using DonationService.Infrastructure.Services;
using DonationService.SharedKernel.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Polly;
using Polly.Extensions.Http;

namespace DonationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgresPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DonationServiceDb")
            ?? throw new InvalidOperationException("Connection string 'DonationServiceDb' is not configured.");

        services.AddDbContext<DonationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "donation_service");
            }));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddMongoPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MongoSettings>()
            .Bind(configuration.GetSection(MongoSettings.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.ConnectionString), "MongoDb:ConnectionString is required.")
            .Validate(s => !string.IsNullOrWhiteSpace(s.DatabaseName), "MongoDb:DatabaseName is required.")
            .ValidateOnStart();

        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = configuration.GetSection(MongoSettings.SectionName).Get<MongoSettings>()
                ?? throw new InvalidOperationException("MongoDb configuration section is missing.");

            return new MongoClient(settings.ConnectionString);
        });

        services.AddScoped<IDonationReadRepository, DonationReadRepository>();

        services.AddHostedService<MongoIndexInitializer>();

        return services;
    }

    public static IServiceCollection AddCampaignServiceClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CampaignServiceOptions>()
            .Bind(configuration.GetSection(CampaignServiceOptions.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.BaseUrl), "CampaignService:BaseUrl is required.")
            .ValidateOnStart();

        var campaignOptions = configuration.GetSection(CampaignServiceOptions.SectionName).Get<CampaignServiceOptions>()
            ?? new CampaignServiceOptions();

        services.AddHttpClient<CampaignServiceHttpClient>(client =>
            {
                client.BaseAddress = new Uri(campaignOptions.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(campaignOptions.TimeoutSeconds);
            })
            .AddPolicyHandler(GetRetryPolicy(campaignOptions.RetryCount))
            .AddPolicyHandler(GetCircuitBreakerPolicy(
                campaignOptions.CircuitBreakerFailureThreshold,
                campaignOptions.CircuitBreakerDurationSeconds));

        services.AddScoped<ICampaignServiceClient>(sp => sp.GetRequiredService<CampaignServiceHttpClient>());

        return services;
    }

    /// <summary>For DonationService.Api: identity extracted from the current HTTP request's validated JWT.</summary>
    public static IServiceCollection AddCurrentUserAndTimeServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }

    /// <summary>For DonationService.Worker: no HTTP request/JWT exists on the consumer side.</summary>
    public static IServiceCollection AddWorkerCurrentUserAndTimeServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, AmbientCorrelationCurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }

    /// <summary>
    /// Producer-side (Api) MassTransit registration: no consumers, but wires
    /// the Entity Framework transactional Bus Outbox against
    /// <see cref="DonationDbContext"/> so publishing only ever hands a
    /// message to RabbitMQ after the surrounding database transaction commits.
    /// </summary>
    public static IServiceCollection AddDonationServiceProducerMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMqSettings = GetRabbitMqSettings(configuration);

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<DonationDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            x.UsingRabbitMq((context, cfg) =>
            {
                ConfigureHost(cfg, rabbitMqSettings);

                cfg.Message<DonationCreatedEvent>(m => m.SetEntityName(DonationCreatedTopology.ExchangeName));
                cfg.Publish<DonationCreatedEvent>(p =>
                {
                    p.Durable = true;
                    // Must match the Worker's e.Bind(...) ExchangeType below -
                    // see DonationCreatedTopology.ExchangeType for why.
                    p.ExchangeType = DonationCreatedTopology.ExchangeType;
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// Consumer-side (Worker) MassTransit registration: registers
    /// <typeparamref name="TConsumer"/> against a single, explicit, durable
    /// queue bound to the DonationCreatedEvent exchange, with PrefetchCount,
    /// ConcurrencyLimit, and a bounded retry policy. Once retries are
    /// exhausted, MassTransit's RabbitMQ transport moves the faulted message
    /// to the conventional "_error" queue - acting as this service's DLQ.
    /// </summary>
    public static IServiceCollection AddDonationServiceConsumerMessaging<TConsumer>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TConsumer : class, MassTransit.IConsumer
    {
        var rabbitMqSettings = GetRabbitMqSettings(configuration);

        services.AddMassTransit(x =>
        {
            x.AddConsumer<TConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                ConfigureHost(cfg, rabbitMqSettings);

                cfg.Message<DonationCreatedEvent>(m => m.SetEntityName(DonationCreatedTopology.ExchangeName));
                // Must be declared here too, matching the Api's
                // cfg.Publish<DonationCreatedEvent> below - otherwise this
                // process's own message topology declares the exchange as
                // MassTransit's default "fanout" while e.Bind below declares
                // it as "direct", and MassTransit throws
                // ConfigurationException ("entity settings did not match the
                // existing entity") building this receive endpoint's topology.
                cfg.Publish<DonationCreatedEvent>(p =>
                {
                    p.Durable = true;
                    p.ExchangeType = DonationCreatedTopology.ExchangeType;
                });

                cfg.ReceiveEndpoint(rabbitMqSettings.DonationCreatedQueueName, e =>
                {
                    e.Durable = true;
                    e.PrefetchCount = rabbitMqSettings.PrefetchCount;
                    e.ConcurrentMessageLimit = rabbitMqSettings.ConcurrencyLimit;

                    e.UseMessageRetry(r => r.Interval(
                        rabbitMqSettings.RetryCount,
                        TimeSpan.FromSeconds(rabbitMqSettings.RetryIntervalSeconds)));

                    e.Bind(DonationCreatedTopology.ExchangeName, s =>
                    {
                        s.RoutingKey = DonationCreatedTopology.RoutingKey;
                        s.ExchangeType = DonationCreatedTopology.ExchangeType;
                    });

                    e.ConfigureConsumer<TConsumer>(context);
                });
            });
        });

        return services;
    }

    private static void ConfigureHost(IRabbitMqBusFactoryConfigurator cfg, RabbitMqSettings settings)
    {
        cfg.Host(settings.Host, settings.Port, settings.VirtualHost, h =>
        {
            h.Username(settings.Username);
            h.Password(settings.Password);
        });
    }

    private static RabbitMqSettings GetRabbitMqSettings(IConfiguration configuration) =>
        configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>()
        ?? throw new InvalidOperationException("RabbitMq configuration section is missing.");

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int failureThreshold, int durationSeconds) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(failureThreshold, TimeSpan.FromSeconds(durationSeconds));
}
