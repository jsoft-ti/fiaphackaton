using CampaignUserService.Domain.Enums;

namespace CampaignUserService.Api.Contracts;

public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? PhotoUrl,
    DateOnly? BirthDate);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);

public sealed record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? PhoneNumber,
    string? Cpf,
    DateOnly? BirthDate,
    RoleName Role);

public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? PhotoUrl,
    DateOnly? BirthDate);

public sealed record ChangeUserRoleRequest(RoleName Role);
