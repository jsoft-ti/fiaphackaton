using Xunit;

namespace CampaignUserService.IntegrationTests.Common;

/// <summary>
/// Shares a single <see cref="CustomWebApplicationFactory"/> (and therefore a
/// single Testcontainers PostgreSQL instance) across every test class in
/// this collection, so the container is only started/stopped once per run.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Integration Tests";
}
