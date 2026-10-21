using RequestsManagement.Api.Startup;
using RequestsManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApi();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured."));

var app = builder.Build();

await app.InitializeDatabaseAsync(args);
if (DatabaseInitializer.IsReseedCommand(args))
    return; // one-off command: `dotnet run -- --reseed [count]`

app.UseExceptionHandler();
app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Requests API"));
app.MapControllers();
app.MapGet("/health", () => Results.Ok("healthy")).ExcludeFromDescription();

app.Run();

public partial class Program; // exposed for WebApplicationFactory in integration tests
