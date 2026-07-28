namespace CampaignUserService.Domain.Enums;

public enum AuditActionType
{
    UserRegistered = 1,
    UserLoggedIn = 2,
    UserLoggedOut = 3,
    PasswordChanged = 4,
    PasswordResetRequested = 5,
    PasswordResetCompleted = 6,
    RoleChanged = 7,
    UserCreated = 8,
    UserUpdated = 9,
    UserDeleted = 10,
    UserActivated = 11,
    UserDeactivated = 12,
    UserBlocked = 13,
    RefreshTokenIssued = 14,
    RefreshTokenRevoked = 15,
    RoleCreated = 16
}
