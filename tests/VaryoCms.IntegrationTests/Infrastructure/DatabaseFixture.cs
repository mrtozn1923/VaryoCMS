using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace VaryoCms.IntegrationTests.Infrastructure;

/// <summary>
/// Starts a SQL Server 2022 container via Testcontainers, creates the test database,
/// and runs all migrations. Shared across a test collection via ICollectionFixture.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("VaryoTest_Dev2024!")
        .WithCleanUp(true)
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Build a connection string pointing at the test database
        string masterConn = _container.GetConnectionString();
        ConnectionString = new SqlConnectionStringBuilder(masterConn)
        {
            InitialCatalog = "VaryoCmsTest",
            TrustServerCertificate = true,
            ConnectTimeout = 60
        }.ConnectionString;

        // Create the test database
        await using var conn = new SqlConnection(masterConn);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "IF DB_ID('VaryoCmsTest') IS NULL CREATE DATABASE VaryoCmsTest;";
        await cmd.ExecuteNonQueryAsync();

        // Resolve migration directory — copied alongside the test DLL
        string? outputDir = Path.GetDirectoryName(typeof(MigrationRunner).Assembly.Location);
        string migrationsDir = Path.Combine(outputDir ?? AppContext.BaseDirectory, "migrations");

        await MigrationRunner.RunAllAsync(ConnectionString, migrationsDir);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition("integration")]
public sealed class IntegrationCollection
    : ICollectionFixture<DatabaseFixture> { }
