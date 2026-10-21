using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestsManagement.Core.Interfaces;
using RequestsManagement.Infrastructure.DAL;
using RequestsManagement.Infrastructure.Services;

namespace RequestsManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // DbContext is Scoped (one per HTTP request); services that use it are Scoped too.
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        // IMemoryCache and TimeProvider are Singletons: shared across requests, thread-safe.
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IRequestQueryService, RequestQueryService>();
        services.AddScoped<IRequestStatusService, RequestStatusService>();
        services.AddScoped<ISummaryService, SummaryService>();
        services.AddScoped<DataSeeder>();

        return services;
    }
}
