using System.Data.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Db2.HealthChecks;

/// <summary>
/// Options used by the Db2 health check.
/// </summary>
public sealed class Db2HealthCheckOptions
{
    /// <summary>
    /// Default lightweight Db2 probe query.
    /// </summary>
    public const string DefaultQuery = "SELECT 1 FROM SYSIBM.SYSDUMMY1";

    /// <summary>
    /// Db2 connection string used by the default connection factory.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Query executed after the connection is opened.
    /// </summary>
    public string Query { get; set; } = DefaultQuery;

    /// <summary>
    /// Maximum duration for the whole health check operation.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// DbCommand.CommandTimeout value, in seconds. Set to null to leave provider default.
    /// </summary>
    public int? CommandTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Failure status reported when the check fails.
    /// </summary>
    public HealthStatus FailureStatus { get; set; } = HealthStatus.Unhealthy;

    /// <summary>
    /// Tags used when registering the health check.
    /// </summary>
    public IEnumerable<string> Tags { get; set; } = new[] { "db2" };

    /// <summary>
    /// Description returned when the check succeeds.
    /// </summary>
    public string HealthyDescription { get; set; } = "DB2 connection successful.";

    /// <summary>
    /// Description returned when the check fails.
    /// </summary>
    public string UnhealthyDescription { get; set; } = "DB2 connection failed.";

    /// <summary>
    /// Include the caught exception in HealthCheckResult. Disable for endpoints where details may be exposed.
    /// </summary>
    public bool IncludeExceptionDetails { get; set; } = true;

    /// <summary>
    /// Disposes the DbConnection after each health check. Keep true unless the factory returns an externally owned connection.
    /// </summary>
    public bool DisposeConnection { get; set; } = true;

    /// <summary>
    /// Optional connection factory. Prefer this for dependency injection, dynamic secrets, or tests.
    /// </summary>
    public Func<IServiceProvider, DbConnection>? ConnectionFactory { get; set; }

    /// <summary>
    /// Optional ADO.NET provider factory. When set, ConnectionString is still required.
    /// </summary>
    public DbProviderFactory? ProviderFactory { get; set; }

    /// <summary>
    /// ADO.NET provider invariant name used with DbProviderFactories. Default is IBM.Data.Db2.
    /// </summary>
    public string ProviderInvariantName { get; set; } = "IBM.Data.Db2";

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            throw new ArgumentException("A Db2 health check query must be provided.", nameof(Query));
        }

        if (Timeout != System.Threading.Timeout.InfiniteTimeSpan && Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(Timeout), Timeout, "Timeout must be positive or System.Threading.Timeout.InfiniteTimeSpan.");
        }

        if (CommandTimeoutSeconds is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CommandTimeoutSeconds), CommandTimeoutSeconds, "Command timeout must be positive when provided.");
        }

        if (ConnectionFactory is null && string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException("ConnectionString must be provided unless ConnectionFactory is configured.");
        }
    }
}
