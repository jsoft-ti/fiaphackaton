namespace CampaignUserService.Application.Features.Users.Dtos;

public sealed record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? Cpf,
    string? PhotoUrl,
    DateOnly? BirthDate,
    string Status,
    bool EmailConfirmed,
    string Role,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? LastLoginAtUtc);

public sealed record UserSummaryDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Status,
    string Role,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc);
