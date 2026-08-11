# Db2.HealthChecks

**Db2.HealthChecks** integrates IBM Db2 probes with `Microsoft.Extensions.Diagnostics.HealthChecks`.

The package is designed to be portable and enterprise-friendly: it does not hard-code an OS-specific IBM driver dependency. Instead, it creates connections through an explicit connection factory, an ADO.NET `DbProviderFactory`, or the IBM Db2 provider loaded by the consuming application.

## Features

- ASP.NET Core / Worker Service health check integration.
- Default lightweight Db2 query: `SELECT 1 FROM SYSIBM.SYSDUMMY1`.
- Custom probe query support.
- Configurable timeout and command timeout.
- Configurable failure status and tags.
- Connection-string, `DbProviderFactory`, or DI-friendly `DbConnection` factory setup.
- Logging through `Microsoft.Extensions.Logging` when logging is registered.
- Multi-target package: `netstandard2.0`, `net8.0`, and `net10.0`.
- SourceLink and deterministic build metadata for NuGet consumers.

## Supported platforms

The library itself is OS-independent. The actual runtime support depends on the IBM Db2 ADO.NET provider used by your application.

Typical provider packages:

| OS | Provider package |
| --- | --- |
| Windows | `Net.IBM.Data.Db2` |
| Linux | `Net.IBM.Data.Db2-lnx` |

> Note: the IBM provider must be installed by the consuming application. This avoids producing NuGet packages whose dependencies change depending on the OS used to pack the library.

### Linux native dependencies

When using `Net.IBM.Data.Db2-lnx`, the IBM native client also needs Linux shared libraries available at runtime. On Debian/Ubuntu-based images this typically means:

```bash
apt-get update && apt-get install -y libxml2 libaio1t64
```

The IBM package copies `clidriver` next to your build output. If `libdb2.so` is not found, ensure the `clidriver/lib` directory is visible to the process, for example via `LD_LIBRARY_PATH` in containerized workloads.

## Basic usage

Install this package and the appropriate IBM provider package for your deployment OS.

```csharp
builder.Services.AddHealthChecks()
    .AddDb2Check(
        name: "db2",
        connectionString: builder.Configuration.GetConnectionString("Db2")!,
        tags: new[] { "db", "db2", "ready" },
        timeout: TimeSpan.FromSeconds(5),
        commandTimeoutSeconds: 3);
```

## Advanced usage

Use the options overload for enterprise scenarios such as dynamic secrets, Key Vault integration, custom connection creation, or non-default failure status.

```csharp
builder.Services.AddHealthChecks()
    .AddDb2Check("db2", options =>
    {
        options.ConnectionString = builder.Configuration.GetConnectionString("Db2")!;
        options.Query = "SELECT 1 FROM SYSIBM.SYSDUMMY1";
        options.Timeout = TimeSpan.FromSeconds(5);
        options.CommandTimeoutSeconds = 3;
        options.FailureStatus = HealthStatus.Degraded;
        options.Tags = new[] { "db", "db2", "critical" };
    });
```

### Custom connection factory

```csharp
builder.Services.AddHealthChecks()
    .AddDb2Check("db2", options =>
    {
        options.ConnectionFactory = serviceProvider =>
        {
            var connectionString = serviceProvider
                .GetRequiredService<IConfiguration>()
                .GetConnectionString("Db2")!;

            // Requires the IBM provider package in the consuming application.
            return new IBM.Data.Db2.DB2Connection(connectionString);
        };
    });
```

### Provider factory

```csharp
builder.Services.AddHealthChecks()
    .AddDb2Check("db2", options =>
    {
        options.ProviderFactory = IBM.Data.Db2.DB2Factory.Instance;
        options.ConnectionString = builder.Configuration.GetConnectionString("Db2")!;
    });
```

## Kubernetes / production endpoints

Recommended pattern:

- `/health/live`: process liveness only, no database dependency.
- `/health/ready`: includes Db2 readiness check, internal/private endpoint.

Example:

```csharp
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
```

Avoid exposing detailed health check output publicly. If needed, set:

```csharp
options.IncludeExceptionDetails = false;
```

## Compatibility notes

- `netstandard2.0` target: supports factory/reflection-based connection creation. `DbProviderFactories.GetFactory` is only used on targets where it is available.
- `net8.0` and `net10.0` targets: support `DbProviderFactories.GetFactory` with `ProviderInvariantName` as well as explicit factories.
- A `DbConnection` returned by `ConnectionFactory` is disposed after every check by default. Set `DisposeConnection = false` only when returning an externally owned connection.
- `IncludeExceptionDetails` defaults to `false`; enable it only on protected diagnostics endpoints.

The library deliberately does not bundle an IBM Db2 driver. Install the provider selected by the
consuming application, such as `Net.IBM.Data.Db2` on Windows or `Net.IBM.Data.Db2-lnx` on Linux.
The provider can be registered with `DbProviderFactories`, supplied through `ProviderFactory`, or
used by a custom `ConnectionFactory`. This keeps IBM driver versions, native dependencies, and
licensing under application control.

## Development

```bash
dotnet restore
dotnet build
dotnet test
dotnet pack src/Db2.HealthChecks/Db2.HealthChecks.csproj -c Release
```

## Security

Do not log or expose Db2 connection strings. See `SECURITY.md` for vulnerability reporting.
