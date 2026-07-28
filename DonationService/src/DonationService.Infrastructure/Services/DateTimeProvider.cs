using DonationService.SharedKernel.Interfaces;

namespace DonationService.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
