# Db2.HealthChecks

**Db2.HealthChecks** is a .NET library designed to integrate **IBM Db2** specific health checks into the standard `Microsoft.Extensions.Diagnostics.HealthChecks` ecosystem.

This library allows developers to easily monitor the connectivity and responsiveness of a Db2 database within .NET applications (such as ASP.NET Core Web APIs or Worker Services).

## Key Features

*   **Simple Integration**: Provides an `AddDb2Check` extension method for `IHealthChecksBuilder`, enabling check registration with a single line of code in your service configuration.
*   **Connectivity Verification**: The check attempts to open a connection to the specified Db2 database.
*   **Test Query Execution**: Executes a lightweight query (default: `SELECT 1 FROM SYSIBM.SYSDUMMY1`) to ensure the database is not only reachable but also capable of processing requests.
*   **Flexibility**:
    *   Supports custom connection strings.
    *   Allows specifying a custom check query (useful for verifying specific tables).
    *   Supports using an existing `DB2Connection` instance registered in Dependency Injection, or creates a new one for each check.
*   **Cross-Platform Support**: Uses `Net.IBM.Data.Db2` (Windows) and `Net.IBM.Data.Db2-lnx` (Linux) packages to ensure compatibility.

## Usage Example

Here is how it typically looks in your `Program.cs`:

```csharp
// Add the Health Check service
builder.Services.AddHealthChecks()
    .AddDb2Check(
        name: "db2_health_check",
        connectionString: "Server=myServer:50000;Database=myDataBase;UID=user;PWD=password;",
        tags: new[] { "db", "sql", "db2" }
    );
```


