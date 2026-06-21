using System.Data.Common;

namespace Db2.HealthChecks;

internal static class DefaultDb2ConnectionFactory
{
    private static readonly string[] Db2ConnectionTypeNames =
    {
        "IBM.Data.Db2.DB2Connection, IBM.Data.Db2",
        "IBM.Data.Db2.DB2Connection, IBM.Data.DB2",
        "IBM.Data.DB2.Core.DB2Connection, IBM.Data.DB2.Core"
    };

    public static DbConnection CreateConnection(Db2HealthCheckOptions options)
    {
        if (options.ProviderFactory is not null)
        {
            return CreateFromProviderFactory(options.ProviderFactory, options.ConnectionString!);
        }

        var providerConnection = TryCreateFromRegisteredProvider(options.ProviderInvariantName, options.ConnectionString!);
        if (providerConnection is not null)
        {
            return providerConnection;
        }

        var reflectionConnection = TryCreateFromKnownDb2Types(options.ConnectionString!);
        if (reflectionConnection is not null)
        {
            return reflectionConnection;
        }

        throw new InvalidOperationException(
            "Unable to create an IBM Db2 connection. Install and load the IBM Db2 ADO.NET provider " +
            "(for example Net.IBM.Data.Db2 on Windows or Net.IBM.Data.Db2-lnx on Linux), " +
            "or configure Db2HealthCheckOptions.ConnectionFactory / ProviderFactory.");
    }

    private static DbConnection CreateFromProviderFactory(DbProviderFactory providerFactory, string connectionString)
    {
        var connection = providerFactory.CreateConnection()
            ?? throw new InvalidOperationException("The configured DbProviderFactory returned null from CreateConnection().");

        connection.ConnectionString = connectionString;
        return connection;
    }

    private static DbConnection? TryCreateFromRegisteredProvider(string providerInvariantName, string connectionString)
    {
#if NETSTANDARD2_0
        _ = providerInvariantName;
        _ = connectionString;
        return null;
#else
        if (string.IsNullOrWhiteSpace(providerInvariantName))
        {
            return null;
        }

        try
        {
            var factory = DbProviderFactories.GetFactory(providerInvariantName);
            return CreateFromProviderFactory(factory, connectionString);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return null;
        }
#endif
    }

    private static DbConnection? TryCreateFromKnownDb2Types(string connectionString)
    {
        foreach (var typeName in Db2ConnectionTypeNames)
        {
            var type = Type.GetType(typeName, throwOnError: false);
            if (type is null)
            {
                continue;
            }

            var connection = Activator.CreateInstance(type, connectionString) as DbConnection;
            if (connection is not null)
            {
                return connection;
            }
        }

        return null;
    }
}
