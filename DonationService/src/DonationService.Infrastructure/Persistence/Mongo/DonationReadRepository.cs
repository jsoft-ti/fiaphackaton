using DonationService.Domain.ReadModels;
using DonationService.Domain.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DonationService.Infrastructure.Persistence.Mongo;

public sealed class DonationReadRepository : IDonationReadRepository
{
    private readonly IMongoCollection<MongoDonationDocument> _collection;

    public DonationReadRepository(IMongoClient mongoClient, IOptions<MongoSettings> options)
    {
        var settings = options.Value;
        var database = mongoClient.GetDatabase(settings.DatabaseName);
        _collection = database.GetCollection<MongoDonationDocument>(settings.DonationsCollectionName);
    }

    public async Task<DonationReadModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _collection
            .Find(d => d.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToReadModel();
    }

    public async Task<(IReadOnlyList<DonationReadModel> Items, long TotalCount)> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var filter = Builders<MongoDonationDocument>.Filter.Eq(d => d.UserId, userId);
        return await QueryPagedAsync(filter, page, pageSize, cancellationToken);
    }

    public async Task<(IReadOnlyList<DonationReadModel> Items, long TotalCount)> GetByCampaignIdAsync(
        Guid campaignId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var filter = Builders<MongoDonationDocument>.Filter.Eq(d => d.CampaignId, campaignId);
        return await QueryPagedAsync(filter, page, pageSize, cancellationToken);
    }

    public async Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var count = await _collection
            .Find(d => d.EventId == eventId)
            .Limit(1)
            .CountDocumentsAsync(cancellationToken);

        return count > 0;
    }

    public async Task UpsertAsync(DonationReadModel donation, CancellationToken cancellationToken)
    {
        var document = MongoDonationDocument.FromReadModel(donation);

        var filter = Builders<MongoDonationDocument>.Filter.Eq(d => d.Id, document.Id);

        await _collection.ReplaceOneAsync(
            filter,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    private async Task<(IReadOnlyList<DonationReadModel> Items, long TotalCount)> QueryPagedAsync(
        FilterDefinition<MongoDonationDocument> filter, int page, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await _collection.Find(filter).CountDocumentsAsync(cancellationToken);

        var documents = await _collection
            .Find(filter)
            .SortByDescending(d => d.DonationDate)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (documents.Select(d => d.ToReadModel()).ToList(), totalCount);
    }
}
