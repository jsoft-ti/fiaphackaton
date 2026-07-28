namespace CampaignUserService.Api.Contracts;

public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword,
    string? PhoneNumber,
    string? Cpf,
    DateOnly? BirthDate);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword, string ConfirmNewPassword);
