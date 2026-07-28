namespace DonationService.Application.Features.Donations.DTOs;

public sealed record DonationDto(
    Guid Id,
    Guid CampaignId,
    Guid UserId,
    string UserName,
    string UserEmail,
    decimal Value,
    string Currency,
    string PaymentMethod,
    DateTime DonationDate,
    string Status,
    DateTime CreatedAtUtc);
