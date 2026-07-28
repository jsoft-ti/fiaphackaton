using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DonationService.IntegrationTests;

/// <summary>
/// Mints JWTs signed with the same symmetric key the test factory configures
/// for the Api under test - simulating tokens issued by CampaignUserService
/// without needing a live instance of it.
/// </summary>
public static class TestJwtTokenFactory
{
    public static string CreateToken(Guid userId, string email, string name, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DonationServiceApiFactory.JwtSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: DonationServiceApiFactory.JwtIssuer,
            audience: DonationServiceApiFactory.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
