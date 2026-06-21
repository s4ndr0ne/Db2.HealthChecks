using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Db2.HealthChecks;

/// <summary>
/// Extension methods for registering IBM Db2 health checks.
/// </summary>
public static class HealthChecksExtensions
{
    /// <summary>
    /// Registers a Db2 health check using a connection string.
    /// </summary>
    public static IHealthChecksBuilder AddDb2Check(
        this IHealthChecksBuilder builder,
        string name,
        string connectionString,
        string query = Db2HealthCheckOptions.DefaultQuery,
        string[]? tags = null,
        HealthStatus? failureStatus = null,
        TimeSpan? timeout = null,
        int? commandTimeoutSeconds = 5)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.AddDb2Check(
            name,
            options =>
            {
                options.ConnectionString = connectionString;
                options.Query = query;
                options.Tags = tags ?? new[] { "db2" };
                options.FailureStatus = failureStatus ?? HealthStatus.Unhealthy;

                if (timeout.HasValue)
                {
                    options.Timeout = timeout.Value;
                }

                options.CommandTimeoutSeconds = commandTimeoutSeconds;
            });
    }

    /// <summary>
    /// Registers a Db2 health check using configurable options.
    /// </summary>
    public static IHealthChecksBuilder AddDb2Check(
        this IHealthChecksBuilder builder,
        string name,
        Action<Db2HealthCheckOptions> configureOptions,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A health check name must be provided.", nameof(name));
        }

        if (configureOptions is null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        var options = new Db2HealthCheckOptions();
        configureOptions(options);

        if (failureStatus.HasValue)
        {
            options.FailureStatus = failureStatus.Value;
        }

        if (tags is not null)
        {
            options.Tags = tags;
        }

        options.Validate();

        return builder.Add(new HealthCheckRegistration(
            name,
            serviceProvider => new Db2HealthCheck(
                options,
                serviceProvider,
                serviceProvider.GetService<ILogger<Db2HealthCheck>>()),
            options.FailureStatus,
            options.Tags));
    }
}
