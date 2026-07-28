using DonationService.Domain.Enums;
using DonationService.Domain.Exceptions;
using DonationService.SharedKernel.Common;

namespace DonationService.Domain.Entities;

/// <summary>
/// Aggregate root representing a donation request on the write (PostgreSQL)
/// side of the service. This is the transactional record created
/// synchronously by the API within the same database transaction as the
/// outbox message - it is NOT the canonical "donation document" (that lives
/// in MongoDB, materialized asynchronously by the Worker once the event is
/// consumed).
/// </summary>
public class Donation : BaseEntity
{
    protected Donation()
    {
        // Required by EF Core.
    }

    private Donation(
        Guid campaignId,
        Guid userId,
        string userName,
        string userEmail,
        decimal value,
        Currency currency,
        PaymentMethod paymentMethod,
        DateTime donationDate,
        Guid correlationId)
    {
        CampaignId = campaignId;
        UserId = userId;
        UserName = userName;
        UserEmail = userEmail;
        Value = value;
        Currency = currency;
        PaymentMethod = paymentMethod;
        DonationDate = donationDate;
        CorrelationId = correlationId;
        EventId = Guid.NewGuid();
        Status = DonationStatus.PendingPublish;
    }

    public Guid CampaignId { get; private set; }

    public Guid UserId { get; private set; }

    public string UserName { get; private set; } = string.Empty;

    public string UserEmail { get; private set; } = string.Empty;

    public decimal Value { get; private set; }

    public Currency Currency { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public DateTime DonationDate { get; private set; }

    public DonationStatus Status { get; private set; }

    /// <summary>Ties this donation back to the originating HTTP request across every downstream service/log.</summary>
    public Guid CorrelationId { get; private set; }

    /// <summary>Id of the DonationCreatedEvent integration event raised for this donation (idempotency key for the consumer).</summary>
    public Guid EventId { get; private set; }

    public static Donation Create(
        Guid campaignId,
        Guid userId,
        string userName,
        string userEmail,
        decimal value,
        Currency currency,
        PaymentMethod paymentMethod,
        DateTime donationDate,
        Guid correlationId)
    {
        if (campaignId == Guid.Empty)
        {
            throw new DomainException("O identificador da campanha é obrigatório.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("O identificador do usuário é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new DomainException("O nome do doador é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            throw new DomainException("O email do doador é obrigatório.");
        }

        if (value <= 0)
        {
            throw new DomainException("O valor da doação deve ser maior que zero.");
        }

        return new Donation(
            campaignId,
            userId,
            userName,
            userEmail,
            value,
            currency,
            paymentMethod,
            donationDate,
            correlationId);
    }

    public void MarkPublished(DateTime utcNow)
    {
        Status = DonationStatus.Published;
        MarkUpdated(utcNow);
    }

    public void MarkPublishFailed(DateTime utcNow)
    {
        Status = DonationStatus.PublishFailed;
        MarkUpdated(utcNow);
    }

    public void MarkConfirmed(DateTime utcNow)
    {
        Status = DonationStatus.Confirmed;
        MarkUpdated(utcNow);
    }
}
