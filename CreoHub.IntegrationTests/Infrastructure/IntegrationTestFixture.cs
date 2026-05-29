using System.Net.Http.Headers;
using CreoHub.Domain.Types;
using CreoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace CreoHub.IntegrationTests.Infrastructure;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("creohub_integration_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private NpgsqlConnection? _resetConnection;
    private Respawner? _respawner;

    public CreoHubApiFactory Factory { get; private set; } = null!;

    public IServiceProvider Services => Factory.Services;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg"));

        await _postgres.StartAsync();

        Factory = new CreoHubApiFactory(_postgres.GetConnectionString());

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }

        _resetConnection = new NpgsqlConnection(_postgres.GetConnectionString());
        await _resetConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_resetConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")],
        });
    }

    public async Task DisposeAsync()
    {
        if (_resetConnection is not null)
            await _resetConnection.DisposeAsync();

        Factory.Dispose();
        await _postgres.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null || _resetConnection is null)
            throw new InvalidOperationException("The integration test database has not been initialized.");

        await _respawner.ResetAsync(_resetConnection);
    }

    public HttpClient CreateClient(Guid? userId = null, UserRole role = UserRole.User)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (userId.HasValue)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.Value.ToString());
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRoleHeader, role.ToString());
        }

        return client;
    }
}
