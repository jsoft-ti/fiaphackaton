namespace CampaignUserService.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string plainTextPassword);

    bool Verify(string plainTextPassword, string passwordHash);
}
