using CampaignUserService.SharedKernel.Interfaces;

namespace CampaignUserService.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
