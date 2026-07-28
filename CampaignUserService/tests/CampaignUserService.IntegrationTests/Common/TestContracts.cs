namespace CampaignUserService.IntegrationTests.Common;

// Lightweight response/request DTOs mirroring the Api's Contracts, kept
// local to the test project so the tests do not need a reference to
// internal Api request/response types beyond what WebApplicationFactory
// already exposes.

public sealed record RegisterRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword,
    string? PhoneNumber = null,
    string? Cpf = null,
    DateOnly? BirthDate = null);

public sealed record LoginRequestDto(string Email, string Password);

public sealed record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    string TokenType,
    Guid UserId,
    string Email,
    string FullName,
    string Role);

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

public sealed record ProblemDetailsDto(string? Title, int? Status, string? Detail);
