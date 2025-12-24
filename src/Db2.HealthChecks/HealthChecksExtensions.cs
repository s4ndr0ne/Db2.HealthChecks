using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Db2.HealthChecks;

public static class HealthChecksExtensions
{
    public static IHealthChecksBuilder AddDb2Check(this IHealthChecksBuilder builder,
           string name,
           string connectionString,
           string query = "SELECT 1 FROM SYSIBM.SYSDUMMY1",
           string[]? tags = null)
    {
        // Register the health check service
        builder.Services.AddKeyedScoped<Db2HealthCheckService>(name, (sp, key) => new Db2HealthCheckService(connectionString, query, sp));

        return builder.Add(new HealthCheckRegistration(
            name,
            provider => provider.GetRequiredKeyedService<Db2HealthCheckService>(name),
            HealthStatus.Unhealthy,
            tags: tags ?? new[] { "db2" }));
    }

}


