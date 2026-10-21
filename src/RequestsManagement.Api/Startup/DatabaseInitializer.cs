using Microsoft.EntityFrameworkCore;
using RequestsManagement.Infrastructure.DAL;

namespace RequestsManagement.Api.Startup;

/// <summary>
/// Prepares the database before the API starts serving:
/// applies migrations (Database:MigrateOnStartup) and creates the test data, either on request
/// (<c>dotnet run -- --reseed [count]</c>) or when the table is empty (Seed:OnStartupIfEmpty).
/// </summary>
public static class DatabaseInitializer
{
    public const string ReseedArgument = "--reseed";

    public static async Task InitializeDatabaseAsync(this WebApplication app, string[] args)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var config = app.Configuration;
        var logger = services.GetRequiredService<ILogger<Program>>();

        if (config.GetValue("Database:MigrateOnStartup", true))
        {
            await services.GetRequiredService<AppDbContext>().Database.MigrateAsync();
            logger.LogInformation("Database migrations applied");
        }

        var seeder = services.GetRequiredService<DataSeeder>();
        if (TryGetReseedCount(args, out var count))
            await seeder.SeedAsync(count, CancellationToken.None);
        else if (config.GetValue("Seed:OnStartupIfEmpty", false) && await seeder.IsEmptyAsync(CancellationToken.None))
            await seeder.SeedAsync(config.GetValue("Seed:Count", DataSeeder.DefaultCount), CancellationToken.None);
    }

    public static bool IsReseedCommand(string[] args) => args.Contains(ReseedArgument);

    private static bool TryGetReseedCount(string[] args, out int count)
    {
        var index = Array.IndexOf(args, ReseedArgument);
        count = index + 1 < args.Length && int.TryParse(args[index + 1], out var n) ? n : DataSeeder.DefaultCount;
        return index >= 0;
    }
}
