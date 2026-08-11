using System.Data;
using System.Data.Common;
using Db2.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Db2.Test;

public class Db2HealthChecksExtensionsTests
{
    [Fact]
    public void AddDb2Check_WithConnectionString_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        var builderMock = new Mock<IHealthChecksBuilder>();
        builderMock.Setup(b => b.Services).Returns(services);

        builderMock.Object.AddDb2Check(
            name: "my-db2-check",
            connectionString: "Server=myServerAddress;Database=myDataBase;",
            tags: new[] { "critical", "database" },
            timeout: TimeSpan.FromSeconds(2),
            commandTimeoutSeconds: 1);

        builderMock.Verify(b => b.Add(It.Is<HealthCheckRegistration>(r =>
            r.Name == "my-db2-check" &&
            r.FailureStatus == HealthStatus.Unhealthy &&
            r.Tags.Contains("critical") &&
            r.Tags.Contains("database")
        )), Times.Once);
    }

    [Fact]
    public async Task AddDb2Check_WithConnectionFactory_ReturnsHealthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddDb2Check("db2", options =>
            {
                options.ConnectionFactory = _ => new SuccessfulDbConnection();
                options.Query = "SELECT 1 FROM SYSIBM.SYSDUMMY1";
            });

        await using var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();

        var report = await healthCheckService.CheckHealthAsync();

        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Equal(HealthStatus.Healthy, report.Entries["db2"].Status);
    }

    [Fact]
    public async Task AddDb2Check_WithFailingConnectionFactory_ReturnsConfiguredFailureStatus()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddDb2Check("db2", options =>
            {
                options.ConnectionFactory = _ => new FailingDbConnection();
                options.FailureStatus = HealthStatus.Degraded;
            });

        await using var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();

        var report = await healthCheckService.CheckHealthAsync();

        Assert.Equal(HealthStatus.Degraded, report.Status);
        Assert.Equal(HealthStatus.Degraded, report.Entries["db2"].Status);
    }

    [Fact]
    public void AddDb2Check_WithoutConnectionStringOrFactory_Throws()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddHealthChecks().AddDb2Check("db2", options => { }));

        Assert.Contains("ConnectionString", exception.Message);
    }

    [Fact]
    public async Task AddDb2Check_DoesNotExposeExceptionDetailsByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddDb2Check("db2", options =>
            {
                options.ConnectionFactory = _ => new FailingDbConnection();
            });

        await using var provider = services.BuildServiceProvider();
        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();

        Assert.Equal(HealthStatus.Unhealthy, report.Entries["db2"].Status);
        Assert.Null(report.Entries["db2"].Exception);
    }

    [Fact]
    public async Task AddDb2Check_AppliesQueryAndCommandTimeout()
    {
        var connection = new SuccessfulDbConnection();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddDb2Check("db2", options =>
            {
                options.ConnectionFactory = _ => connection;
                options.Query = "SELECT 42";
                options.CommandTimeoutSeconds = 7;
            });

        await using var provider = services.BuildServiceProvider();
        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();

        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Equal("SELECT 42", connection.LastCommandText);
        Assert.Equal(7, connection.LastCommandTimeout);
    }

    [Fact]
    public async Task AddDb2Check_TimesOutAndDisposesConnection()
    {
        var connection = new BlockingDbConnection();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddDb2Check("db2", options =>
            {
                options.ConnectionFactory = _ => connection;
                options.Timeout = TimeSpan.FromMilliseconds(20);
            });

        await using var provider = services.BuildServiceProvider();
        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();

        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Contains("Timed out", report.Entries["db2"].Description);
        Assert.True(connection.WasDisposed);
    }

#pragma warning disable CS8764, CS8765 // Test doubles intentionally implement BCL provider contracts across TFMs.
    private class SuccessfulDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        public override string? ConnectionString { get; set; } = string.Empty;
        public override string Database => "test";
        public override string DataSource => "test";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _state;
        public string? LastCommandText { get; private set; }
        public int LastCommandTimeout { get; private set; }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            _state = ConnectionState.Open;
            return Task.CompletedTask;
        }

        public override void Open() => _state = ConnectionState.Open;
        public override void Close() => _state = ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new SuccessfulDbCommand(this);

        internal void RecordCommand(SuccessfulDbCommand command)
        {
            LastCommandText = command.CommandText;
            LastCommandTimeout = command.CommandTimeout;
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }

        public bool WasDisposed { get; private set; }
    }

    private sealed class FailingDbConnection : SuccessfulDbConnection
    {
        public override Task OpenAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("Cannot connect.");
        public override void Open() => throw new InvalidOperationException("Cannot connect.");
    }

    private sealed class SuccessfulDbCommand : DbCommand
    {
        private readonly SuccessfulDbConnection _connection;

        public SuccessfulDbCommand(SuccessfulDbConnection connection) => _connection = connection;
        public override string? CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; } = new EmptyDbParameterCollection();
        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel() { }
        public override int ExecuteNonQuery() => 1;
        public override object ExecuteScalar()
        {
            _connection.RecordCommand(this);
            return 1;
        }
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => throw new NotSupportedException();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
    }

    private sealed class BlockingDbConnection : SuccessfulDbConnection
    {
        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    private sealed class EmptyDbParameterCollection : DbParameterCollection
    {
        public override int Count => 0;
        public override object SyncRoot { get; } = new();
        public override int Add(object value) => throw new NotSupportedException();
        public override void AddRange(Array values) => throw new NotSupportedException();
        public override void Clear() { }
        public override bool Contains(object value) => false;
        public override bool Contains(string value) => false;
        public override void CopyTo(Array array, int index) { }
        public override IEnumerator<object> GetEnumerator() => Enumerable.Empty<object>().GetEnumerator();
        public override int IndexOf(object value) => -1;
        public override int IndexOf(string parameterName) => -1;
        public override void Insert(int index, object value) => throw new NotSupportedException();
        public override void Remove(object value) => throw new NotSupportedException();
        public override void RemoveAt(int index) => throw new NotSupportedException();
        public override void RemoveAt(string parameterName) => throw new NotSupportedException();
        protected override DbParameter GetParameter(int index) => throw new IndexOutOfRangeException();
        protected override DbParameter GetParameter(string parameterName) => throw new IndexOutOfRangeException();
        protected override void SetParameter(int index, DbParameter value) => throw new NotSupportedException();
        protected override void SetParameter(string parameterName, DbParameter value) => throw new NotSupportedException();
    }
#pragma warning restore CS8764, CS8765
}
