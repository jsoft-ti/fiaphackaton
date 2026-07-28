using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CampaignUserService.Infrastructure.Persistence;

/// <summary>
/// Enables "dotnet ef migrations add/update" to be run from the command
/// line without needing to spin up the full Api host. Reads the connection
/// string from environment variables or a local appsettings file placed
/// next to this project (falls back to a placeholder if none is found -
/// migrations can still be generated, only "database update" needs a real
/// connection string).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=campaign_user_service;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", "campaign_user"));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
