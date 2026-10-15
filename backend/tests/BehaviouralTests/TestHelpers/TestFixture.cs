using System.Data.Common;
using Infrastructure;
using Infrastructure.BackgroundServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OpenTelemetry.Trace;
using Presentation;
using Respawn;

namespace BehaviouralTests.TestHelpers;

public class TestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ConnectionStringKey = $"ConnectionStrings:{TransactionMonitoringDbContext.DatabaseConnectionName}";

    private string _connectionString = null!;
    private DbConnection _dbConnection = default!;
    private Respawner _respawner = null!;

    public CommandCountingInterceptor CommandCounter { get; } = new();

    public async Task InitializeAsync()
    {
        _connectionString = await TestDatabase.CreateAsync();

        _dbConnection = new NpgsqlConnection(_connectionString);
        await _dbConnection.OpenAsync();

        _respawner = await RespawnerHelper.CreateRespawnerAsync(_dbConnection);

        GuardAgainstSharedDatabase();

        // TestServer drops the caller's execution context by default, and without it the command
        // counter's async-scoped window never reaches the code handling the request — it would count
        // nothing at all, which reads as a passing assertion rather than a broken one.
        Server.PreserveExecutionContext = true;
    }

    public new async Task DisposeAsync()
    {
        await _dbConnection.DisposeAsync();

        await base.DisposeAsync();

        await TestDatabase.DropAsync(_connectionString);
    }

    public async Task ResetDatabaseAsync()
    {
        await DeleteComposedRolesAsync();

        await _respawner.ResetAsync(_dbConnection);

        await RestoreSeededRolesAsync();
    }

    // Composed roles must go before the reset or their FK to Organizations blocks the truncate. TRUNCATE ... CASCADE on Organizations still empties Roles despite the ignore list, so the built-in four are reseeded after.
    private async Task RestoreSeededRolesAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        if (await context.Roles.AnyAsync())
        {
            return;
        }

        await context.Roles.AddRangeAsync(SeedData.CreateIdentityRoles());
        await context.RolePermissions.AddRangeAsync(SeedData.CreateBuiltInRolePermissions());

        await context.SaveChangesAsync();
    }

    private async Task DeleteComposedRolesAsync()
    {
        await using var command = _dbConnection.CreateCommand();

        command.CommandText = """DELETE FROM "Identity"."Roles" WHERE "OrganizationId" IS NOT NULL;""";

        await command.ExecuteNonQueryAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseConfiguration(ConfigurationHelper.Config);
        builder.UseSetting(ConnectionStringKey, _connectionString);

        builder.ConfigureTestServices(services =>
        {
            services.AddHostedService<OutboxBackgroundService>();

            // Every test creates a user and logs in, so the production iteration count would
            // dominate the run time. Verification still honours whatever the stored hash encodes.
            services.Configure<PasswordHasherOptions>(options => options.IterationCount = 1);

            // Debug builds export traces with ExportProcessorType.Simple, which makes every span a
            // blocking OTLP round trip. Without the provider there is no listener, so the
            // instrumentation never records a span in the first place.
            services.RemoveAll<TracerProvider>();

            // Respawner resets the Identity and Service schemas but not the Quartz one, so a job a
            // test schedules stays persisted and the live scheduler fires it during a later test,
            // against rows that have since been wiped. Every test drives its jobs directly.
            services.Remove(services.Single(descriptor =>
                descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType?.FullName == "Quartz.QuartzHostedService"));

            // EF resolves IInterceptor from the application service provider, so this reaches the
            // DbContext without the production registration knowing about it. It only counts while
            // a test explicitly starts it.
            services.AddSingleton<IInterceptor>(CommandCounter);
        });
    }

    // Test classes run in parallel against a database each. If the host ever fell back to a
    // connection string from configuration they would silently share one and corrupt each other.
    private void GuardAgainstSharedDatabase()
    {
        var hostConnectionString = Services.GetRequiredService<IConfiguration>()
            .GetConnectionString(TransactionMonitoringDbContext.DatabaseConnectionName);

        if (hostConnectionString != _connectionString)
        {
            throw new InvalidOperationException(
                $"The test host resolved '{hostConnectionString}' instead of its own database '{_connectionString}'.");
        }
    }
}
