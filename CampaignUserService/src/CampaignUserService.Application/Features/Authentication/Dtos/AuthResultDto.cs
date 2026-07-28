namespace CampaignUserService.Application.Features.Authentication.Dtos;

public sealed record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    string TokenType,
    Guid UserId,
    string Email,
    string FullName,
    string Role);
