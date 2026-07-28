using CampaignUserService.Application.Common.Interfaces;

namespace CampaignUserService.Infrastructure.Security;

/// <summary>BCrypt-based password hasher (work factor 12).</summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainTextPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainTextPassword, WorkFactor);

    public bool Verify(string plainTextPassword, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
