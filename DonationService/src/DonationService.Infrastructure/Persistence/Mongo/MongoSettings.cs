using System.ComponentModel.DataAnnotations;

namespace DonationService.Infrastructure.Persistence.Mongo;

public sealed class MongoSettings
{
    public const string SectionName = "MongoDb";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    [Required]
    public string DatabaseName { get; init; } = "donation_service";

    public string DonationsCollectionName { get; init; } = "donations";
}
