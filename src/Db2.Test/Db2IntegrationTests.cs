using System;
using Db2.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Testcontainers.Db2;
using Xunit;

namespace Db2.Test;

public sealed class Db2IntegrationTests : IAsyncLifetime
{
    private readonly bool _enabled = IsIntegrationEnabled;
    private Db2Container? _container;

    public async Task InitializeAsync()
    {
        _container = null;
        if (!_enabled)
        {
            return;
        }

        var builder = new Db2Builder("icr.io/db2_community/db2:12.1.0.0")
            .WithDatabase("testdb")
            .WithUsername("db2admin")
            .WithPassword("your_password_here")
            .WithAcceptLicenseAgreement(true);

        _container = builder.Build();
        await _container.StartAsync();
    }

    [SkippableFact]
    public async Task AddDb2Check_RealDb2_ReturnsHealthy()
    {
        EnsureContainerReady();
        var connectionString = _container!.GetConnectionString();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddDb2Check("db2", connectionString);

        await using var provider = services.BuildServiceProvider();
        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();

        Assert.Equal(HealthStatus.Healthy, report.Status);
    }

    [SkippableFact]
    public async Task AddDb2Check_RealDb2_CustomQuery_ReturnsHealthy()
    {
        EnsureContainerReady();
        var connectionString = _container!.GetConnectionString();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddDb2Check("db2", connectionString, query: "SELECT 1 FROM SYSIBM.SYSDUMMY1");

        await using var provider = services.BuildServiceProvider();
        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();

        Assert.Equal(HealthStatus.Healthy, report.Status);
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    private static bool IsIntegrationEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("RUN_DB2_INTEGRATION_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    private void EnsureContainerReady()
    {
        Skip.IfNot(_enabled && _container is not null, "Set RUN_DB2_INTEGRATION_TESTS=true with Docker available to run Db2 integration tests.");
    }
}
