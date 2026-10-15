using Npgsql;
using Testcontainers.PostgreSql;

namespace BehaviouralTests.TestHelpers;

internal static class TestDatabase
{
    private const string Image = "postgres:15.4-bullseye";
    private const string Username = "postgres";
    private const string Password = "postgres";
    private const string TemplateDatabase = "transaction_monitoring_template";
    private const string MaintenanceDatabase = "postgres";
    private const string MigrationScript = "Migration.sql";

    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static PostgreSqlContainer? _container;
    private static int _databaseNumber;

    public static async Task<string> CreateAsync()
    {
        await Gate.WaitAsync();

        try
        {
            _container ??= await StartAsync();

            var database = $"behavioural_tests_{Interlocked.Increment(ref _databaseNumber)}";

            // FILE_COPY rather than the default WAL_LOG, which writes the whole template into WAL on
            // every copy. WAL lives on the same tmpfs as the data, and on a CI runner it filled the disk.
            await ExecuteAsync(ConnectionStringFor(_container, MaintenanceDatabase),
                $"""CREATE DATABASE "{database}" TEMPLATE "{TemplateDatabase}" STRATEGY FILE_COPY;""");

            return ConnectionStringFor(_container, database);
        }
        finally
        {
            Gate.Release();
        }
    }

    // The data directory is a tmpfs sized from host memory, so a database left behind by every test
    // class runs a small CI runner out of space partway through the suite.
    public static async Task DropAsync(string connectionString)
    {
        if (_container is null)
        {
            return;
        }

        var database = new NpgsqlConnectionStringBuilder(connectionString).Database;

        await ExecuteAsync(ConnectionStringFor(_container, MaintenanceDatabase),
            $"""DROP DATABASE IF EXISTS "{database}" WITH (FORCE);""");
    }

    private static async Task<PostgreSqlContainer> StartAsync()
    {
        var container = new PostgreSqlBuilder(Image)
            .WithDatabase(TemplateDatabase)
            .WithUsername(Username)
            .WithPassword(Password)
            .WithTmpfsMount("/var/lib/postgresql/data")
            .WithCommand(
                "-c", "fsync=off",
                "-c", "synchronous_commit=off",
                "-c", "full_page_writes=off",
                "-c", "wal_level=minimal",
                "-c", "max_wal_senders=0",
                "-c", "autovacuum=off",
                "-c", "checkpoint_timeout=30min",
                "-c", "max_wal_size=512MB",
                "-c", "shared_buffers=256MB",
                "-c", "max_connections=500")
            .Build();

        await container.StartAsync();

        var migration = await File.ReadAllTextAsync(MigrationScript);

        // The template must have no other sessions attached or CREATE DATABASE ... TEMPLATE fails,
        // so this connection stays out of the pool.
        await ExecuteAsync(ConnectionStringFor(container, TemplateDatabase, pooling: false), migration);

        return container;
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.CommandTimeout = 120;

        await command.ExecuteNonQueryAsync();
    }

    private static string ConnectionStringFor(PostgreSqlContainer container, string database, bool pooling = true)
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host = container.Hostname,
            Port = container.GetMappedPublicPort(PostgreSqlBuilder.PostgreSqlPort),
            Username = Username,
            Password = Password,
            Database = database,
            IncludeErrorDetail = true,
            Pooling = pooling,
            MaxPoolSize = 20
        }.ConnectionString;
    }
}
