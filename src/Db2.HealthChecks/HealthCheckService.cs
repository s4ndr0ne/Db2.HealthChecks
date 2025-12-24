using Microsoft.Extensions.Diagnostics.HealthChecks;
using IBM.Data.Db2;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace Db2.HealthChecks;

internal class Db2HealthCheckService : IHealthCheck
{
    private readonly string _connectionString;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _query;

    public Db2HealthCheckService(string connectionString, string query, IServiceProvider serviceProvider)
    {
        _connectionString = connectionString;
        _query = query;
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = _serviceProvider.GetService<DB2Connection>();

            if (connection != null)
            {
                await CheckConnectionAsync(connection, cancellationToken);
            }
            else
            {
                await using var newConnection = new DB2Connection(_connectionString);
                await CheckConnectionAsync(newConnection, cancellationToken);
            }

            return HealthCheckResult.Healthy("DB2 Connection Successful");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(description: "DB2 Connection Failed", exception: ex);
        }
    }

    private async Task CheckConnectionAsync(DB2Connection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        using var command = connection.CreateCommand();
        command.CommandText = _query;
        await command.ExecuteScalarAsync(cancellationToken);
    }
}