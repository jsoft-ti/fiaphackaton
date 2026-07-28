using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Exceptions;
using CampaignUserService.SharedKernel.Common;

namespace CampaignUserService.Domain.Entities;

/// <summary>
/// Aggregate root representing a system user (Doador or GestorOng).
/// Encapsulates all state transitions so invalid states can never be
/// represented outside of this class (DDD rich-domain-model approach).
/// </summary>
public class User : BaseEntity
{
    private readonly List<UserRole> _userRoles = [];

    protected User()
    {
        // Required by EF Core.
    }

    private User(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        string? phoneNumber,
        string? cpf,
        DateOnly? birthDate)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        PhoneNumber = phoneNumber;
        Cpf = cpf;
        BirthDate = birthDate;
        Status = UserStatus.Active;
        EmailConfirmed = false;
    }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public string? Cpf { get; private set; }

    public string? PhotoUrl { get; private set; }

    public DateOnly? BirthDate { get; private set; }

    public UserStatus Status { get; private set; }

    public bool EmailConfirmed { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    public int AccessFailedCount { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public static User Create(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        string? phoneNumber,
        string? cpf,
        DateOnly? birthDate)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("O nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("O sobrenome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("O email é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("O hash de senha é obrigatório.");
        }

        return new User(firstName, lastName, email, passwordHash, phoneNumber, cpf, birthDate);
    }

    public void AssignRole(Role role, DateTime utcNow)
    {
        if (_userRoles.Any(ur => ur.RoleId == role.Id))
        {
            return;
        }

        _userRoles.Add(UserRole.Create(Id, role.Id));
        MarkUpdated(utcNow);
    }

    public void RemoveRole(Guid roleId, DateTime utcNow)
    {
        var existing = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (existing is null)
        {
            return;
        }

        _userRoles.Remove(existing);
        MarkUpdated(utcNow);
    }

    public void ReplaceRole(Role newRole, DateTime utcNow)
    {
        _userRoles.Clear();
        _userRoles.Add(UserRole.Create(Id, newRole.Id));
        MarkUpdated(utcNow);
    }

    public void UpdateProfile(
        string firstName,
        string lastName,
        string? phoneNumber,
        string? photoUrl,
        DateOnly? birthDate,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("O nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("O sobrenome é obrigatório.");
        }

        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        PhotoUrl = photoUrl;
        BirthDate = birthDate;
        MarkUpdated(utcNow);
    }

    public void ChangePassword(string newPasswordHash, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new DomainException("O hash de senha é obrigatório.");
        }

        PasswordHash = newPasswordHash;
        MarkUpdated(utcNow);
    }

    public void ConfirmEmail(DateTime utcNow)
    {
        EmailConfirmed = true;
        MarkUpdated(utcNow);
    }

    public void RecordSuccessfulLogin(DateTime utcNow)
    {
        LastLoginAtUtc = utcNow;
        AccessFailedCount = 0;
        MarkUpdated(utcNow);
    }

    public void RecordFailedLoginAttempt(DateTime utcNow)
    {
        AccessFailedCount++;
        MarkUpdated(utcNow);
    }

    public void Activate(DateTime utcNow)
    {
        if (Status == UserStatus.Active)
        {
            return;
        }

        Status = UserStatus.Active;
        MarkUpdated(utcNow);
    }

    public void Deactivate(DateTime utcNow)
    {
        if (Status == UserStatus.Inactive)
        {
            return;
        }

        Status = UserStatus.Inactive;
        MarkUpdated(utcNow);
    }

    public void Block(DateTime utcNow)
    {
        if (Status == UserStatus.Blocked)
        {
            return;
        }

        Status = UserStatus.Blocked;
        MarkUpdated(utcNow);
    }

    public bool CanAuthenticate() => Status == UserStatus.Active && !IsDeleted;

    public void SoftDelete(DateTime utcNow) => MarkDeleted(utcNow);
}
