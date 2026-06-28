using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace VaryoCms.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory that overrides the connection string and key settings
/// so integration tests run against the Testcontainers SQL Server instance.
/// Uses the in-memory TestServer (no real Kestrel needed for HTTP-only integration tests).
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:VaryoCms"]    = _connectionString,
                ["Jwt:SigningKey"]                 = "test-signing-key-at-least-32-bytes-long!!",
                ["Jwt:Issuer"]                    = "VaryoCms",
                ["DevTenantSlug"]                 = "dev-tenant",
                // Disable Serilog MSSqlServer sink in tests (console only)
                ["Serilog:WriteTo:0:Name"]        = "Console",
                ["EmailVerification:Enabled"]     = "false",
            });
        });
    }
}
