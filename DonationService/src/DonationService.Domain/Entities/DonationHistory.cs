using DonationService.Domain.Enums;
using DonationService.SharedKernel.Common;

namespace DonationService.Domain.Entities;

/// <summary>
/// Append-only audit trail of every status transition a <see cref="Donation"/>
/// goes through. Written in PostgreSQL, in the same transaction as the
/// donation and its outbox message, so the audit record can never
/// desynchronize from the donation itself.
/// </summary>
public class DonationHistory : BaseEntity
{
    protected DonationHistory()
    {
    }

    private DonationHistory(
        Guid donationId,
        DonationStatus previousStatus,
        DonationStatus newStatus,
        string description)
    {
        DonationId = donationId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Description = description;
        OccurredAtUtc = DateTime.UtcNow;
    }

    public Guid DonationId { get; private set; }

    public Donation? Donation { get; private set; }

    public DonationStatus PreviousStatus { get; private set; }

    public DonationStatus NewStatus { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTime OccurredAtUtc { get; private set; }

    public static DonationHistory Create(
        Guid donationId,
        DonationStatus previousStatus,
        DonationStatus newStatus,
        string description) =>
        new(donationId, previousStatus, newStatus, description);
}
