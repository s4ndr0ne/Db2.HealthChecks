using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Db2.HealthChecks;

/// <summary>
/// Shared probe state for a single health check registration. A new <see cref="Db2HealthCheck"/>
/// instance is created per execution by the health check infrastructure, so caching and
/// single-flighting must live here to survive across probes. Lifetime is the application lifetime.
/// </summary>
internal sealed class Db2ProbeCache
{
    public readonly SemaphoreSlim Gate = new(1, 1);
    public HealthCheckResult? Result;
    public DateTimeOffset At;
}

internal sealed class Db2HealthCheck : IHealthCheck
{
    private readonly Db2HealthCheckOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Db2HealthCheck>? _logger;
    private readonly Db2ProbeCache _cache;

    public Db2HealthCheck(
        Db2HealthCheckOptions options,
        IServiceProvider serviceProvider,
        Db2ProbeCache cache,
        ILogger<Db2HealthCheck>? logger = null)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        _cache = cache;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_options.CacheDuration <= TimeSpan.Zero)
        {
            return await ProbeAsync(context, cancellationToken).ConfigureAwait(false);
        }

        await _cache.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache.Result is not null && DateTimeOffset.UtcNow - _cache.At < _options.CacheDuration)
            {
                _logger?.LogDebug("Db2 health check '{Name}' returning cached result.", context.Registration.Name);
                return _cache.Result.Value;
            }

            var result = await ProbeAsync(context, cancellationToken).ConfigureAwait(false);
            _cache.Result = result;
            _cache.At = DateTimeOffset.UtcNow;
            return result;
        }
        finally
        {
            _cache.Gate.Release();
        }
    }

    private async Task<HealthCheckResult> ProbeAsync(HealthCheckContext context, CancellationToken cancellationToken)
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
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
            if (_options.DisposeConnection && connection is not null)
            {
                await DisposeConnectionAsync(connection).ConfigureAwait(false);
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

#if NETSTANDARD2_0
        using var command = connection.CreateCommand();
#else
        await using var command = connection.CreateCommand();
#endif
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

    private static Task DisposeConnectionAsync(DbConnection connection)
    {
#if NETSTANDARD2_0
        connection.Dispose();
        return Task.CompletedTask;
#else
        return connection.DisposeAsync().AsTask();
#endif
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
