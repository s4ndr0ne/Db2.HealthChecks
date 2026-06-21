using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Db2.HealthChecks;

internal sealed class Db2HealthCheck : IHealthCheck
{
    private readonly Db2HealthCheckOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Db2HealthCheck>? _logger;

    public Db2HealthCheck(
        Db2HealthCheckOptions options,
        IServiceProvider serviceProvider,
        ILogger<Db2HealthCheck>? logger = null)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeoutTokenSource = CreateTimeoutTokenSource(cancellationToken);
        var effectiveCancellationToken = timeoutTokenSource?.Token ?? cancellationToken;

        DbConnection? connection = null;

        try
        {
            connection = CreateConnection();
            await CheckConnectionAsync(connection, effectiveCancellationToken).ConfigureAwait(false);

            _logger?.LogDebug("Db2 health check '{Name}' completed successfully.", context.Registration.Name);
            return HealthCheckResult.Healthy(_options.HealthyDescription);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && _options.Timeout != System.Threading.Timeout.InfiniteTimeSpan)
        {
            _logger?.LogWarning(ex, "Db2 health check '{Name}' timed out after {Timeout}.", context.Registration.Name, _options.Timeout);
            return CreateFailureResult($"{_options.UnhealthyDescription} Timed out after {_options.Timeout}.", ex);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Db2 health check '{Name}' failed.", context.Registration.Name);
            return CreateFailureResult(_options.UnhealthyDescription, ex);
        }
        finally
        {
            if (_options.DisposeConnection)
            {
                connection?.Dispose();
            }
        }
    }

    private DbConnection CreateConnection()
    {
        var connection = _options.ConnectionFactory?.Invoke(_serviceProvider)
            ?? DefaultDb2ConnectionFactory.CreateConnection(_options);

        if (connection is null)
        {
            throw new InvalidOperationException("The Db2 connection factory returned null.");
        }

        return connection;
    }

    private async Task CheckConnectionAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        using var command = connection.CreateCommand();
        command.CommandText = _options.Query;

        if (_options.CommandTimeoutSeconds.HasValue)
        {
            command.CommandTimeout = _options.CommandTimeoutSeconds.Value;
        }

        await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }

    private HealthCheckResult CreateFailureResult(string description, Exception exception)
    {
        return new HealthCheckResult(
            _options.FailureStatus,
            description,
            _options.IncludeExceptionDetails ? exception : null);
    }

    private CancellationTokenSource? CreateTimeoutTokenSource(CancellationToken cancellationToken)
    {
        if (_options.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
        {
            return null;
        }

        var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutTokenSource.CancelAfter(_options.Timeout);
        return timeoutTokenSource;
    }
}
