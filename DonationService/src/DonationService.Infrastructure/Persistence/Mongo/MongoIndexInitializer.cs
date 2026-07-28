using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DonationService.Infrastructure.Persistence.Mongo;

/// <summary>
/// Creates the required indexes on the "donations" collection on startup:
/// CampaignId, UserId, DonationDate, Status (and a unique EventId index for
/// consumer idempotency). <c>CreateManyAsync</c> is idempotent - re-running
/// it (e.g. because both the Api and the Worker host this) is a no-op once
/// the indexes already exist with the same keys/options.
/// </summary>
public sealed class MongoIndexInitializer : IHostedService
{
    private readonly IMongoClient _mongoClient;
    private readonly MongoSettings _settings;
    private readonly ILogger<MongoIndexInitializer> _logger;

    public MongoIndexInitializer(
        IMongoClient mongoClient,
        IOptions<MongoSettings> options,
        ILogger<MongoIndexInitializer> logger)
    {
        _mongoClient = mongoClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var database = _mongoClient.GetDatabase(_settings.DatabaseName);
        var collection = database.GetCollection<MongoDonationDocument>(_settings.DonationsCollectionName);

        var indexModels = new List<CreateIndexModel<MongoDonationDocument>>
        {
            new(Builders<MongoDonationDocument>.IndexKeys.Ascending(d => d.CampaignId),
                new CreateIndexOptions { Name = "ix_donations_campaignId" }),
            new(Builders<MongoDonationDocument>.IndexKeys.Ascending(d => d.UserId),
                new CreateIndexOptions { Name = "ix_donations_userId" }),
            new(Builders<MongoDonationDocument>.IndexKeys.Descending(d => d.DonationDate),
                new CreateIndexOptions { Name = "ix_donations_donationDate" }),
            new(Builders<MongoDonationDocument>.IndexKeys.Ascending(d => d.Status),
                new CreateIndexOptions { Name = "ix_donations_status" }),
            new(Builders<MongoDonationDocument>.IndexKeys.Ascending(d => d.EventId),
                new CreateIndexOptions { Name = "ux_donations_eventId", Unique = true }),
        };

        await collection.Indexes.CreateManyAsync(indexModels, cancellationToken);

        _logger.LogInformation(
            "MongoDB indexes ensured on {Database}.{Collection}",
            _settings.DatabaseName,
            _settings.DonationsCollectionName);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
