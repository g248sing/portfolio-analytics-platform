using Testcontainers.PostgreSql;
using Xunit;

namespace Portfolio.IntegrationTests;

public class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("portfolio")
        .WithUsername("portfolio")
        .WithPassword("portfolio")
        .Build();

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<PostgresFixture>;
