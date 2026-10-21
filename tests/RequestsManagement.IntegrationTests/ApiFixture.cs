using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RequestsManagement.Core.DTOs;
using RequestsManagement.Infrastructure.DAL;
using Testcontainers.MsSql;

namespace RequestsManagement.IntegrationTests;

/// <summary>
/// One SQL Server container + one API host for the whole test run. The schema is created by the real
/// migrations and the data by the real seeder, so the tests exercise the same SQL that production runs.
/// Requires Docker, unless INTEGRATION_SQL holds a connection string to a throwaway database (it is wiped).
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public const int SeedCount = 2_000;

    private static readonly string? ExternalConnectionString = Environment.GetEnvironmentVariable("INTEGRATION_SQL");

    private readonly MsSqlContainer? _sql = ExternalConnectionString is null ? new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build() : null;
    private WebApplicationFactory<Program> _factory = null!;

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public HttpClient Client { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (_sql is not null)
            await _sql.StartAsync();
        var connectionString = ConnectionString = _sql?.GetConnectionString() ?? ExternalConnectionString!;

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", connectionString);
            builder.UseSetting("Seed:OnStartupIfEmpty", "false");
        });
        Client = _factory.CreateClient(); // starts the host, which applies migrations

        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync(SeedCount, CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        if (_sql is not null)
            await _sql.DisposeAsync();
    }

    public async Task<RequestDto> GetRequestAsync(int id) =>
        (await Client.GetFromJsonAsync<RequestDto>($"/api/requests/{id}", Json))!;
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
