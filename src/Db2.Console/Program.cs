using Db2.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Testcontainers.Db2;

namespace Db2.ConsoleApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        System.Environment.SetEnvironmentVariable("DOCKER_API_VERSION", "1.41");
        System.Environment.SetEnvironmentVariable("DOCKER_CLIENT_VERSION", "1.41");
        var db2Container = new Db2Builder()
            .WithDatabase("testdb")
            .WithUsername("db2admin")
            .WithPassword("your_password_here")
            .WithImage("icr.io/db2_community/db2:12.1.0.0")
            .WithAcceptLicenseAgreement(true)
            .Build();

        await db2Container.StartAsync();

        var db2ConnectionString = db2Container.GetConnectionString();

        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddConsole());       
        
        services.AddHealthChecks()
                .AddDb2Check("db2_check", db2ConnectionString);

        var serviceProvider = services.BuildServiceProvider();

        // Retrieve the HealthCheckService
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        Console.WriteLine("Executing Health Check...");

        // Execute the health check
        var report = await healthCheckService.CheckHealthAsync();

        // Print the results
        Console.WriteLine($"Overall Status: {report.Status}");
        
        foreach (var entry in report.Entries)
        {
            Console.WriteLine($"Check: {entry.Key}");
            Console.WriteLine($" - Status: {entry.Value.Status}");
            Console.WriteLine($" - Description: {entry.Value.Description}");
            if (entry.Value.Exception != null)
            {
                Console.WriteLine($" - Error: {entry.Value.Exception.Message}");
            }
        }
    }
}