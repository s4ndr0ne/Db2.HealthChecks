using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;
using Db2.HealthChecks;
namespace Db2.Test;

public class UnitTest1
{
    [Fact]
    public void AddDb2Check_RegistersKeyedServiceAndHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var builderMock = new Mock<IHealthChecksBuilder>();
        builderMock.Setup(b => b.Services).Returns(services);
        
        var healthCheckName = "my-db2-check";
        var connectionString = "Server=myServerAddress;Database=myDataBase;";

        // Act
        builderMock.Object.AddDb2Check(healthCheckName, connectionString);

        // Assert
        // 1. Verify Service Registration
        var descriptor = services.FirstOrDefault(s => 
            s.ServiceType.Name == "Db2HealthCheckService" && 
            s.IsKeyedService && 
            s.ServiceKey?.ToString() == healthCheckName);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);

        // 2. Verify Health Check Registration via Builder
        builderMock.Verify(b => b.Add(It.Is<HealthCheckRegistration>(r => 
            r.Name == healthCheckName &&
            r.FailureStatus == HealthStatus.Unhealthy &&
            r.Tags.Contains("db2")
        )), Times.Once);
    }

    [Fact]
    public void AddDb2Check_WithCustomTags_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var builderMock = new Mock<IHealthChecksBuilder>();
        builderMock.Setup(b => b.Services).Returns(services);
        
        var tags = new[] { "critical", "database" };

        // Act
        builderMock.Object.AddDb2Check("db2", "connStr", tags: tags);

        // Assert
        builderMock.Verify(b => b.Add(It.Is<HealthCheckRegistration>(r => 
            r.Tags.Contains("critical") && 
            r.Tags.Contains("database")
        )), Times.Once);
    }
}
