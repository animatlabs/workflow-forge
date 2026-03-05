using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Extensions.Persistence;

namespace WorkflowForge.Extensions.Persistence.Tests;

public class ServiceCollectionExtensionsShould
{
    [Fact]
    public void ThrowArgumentNullException_GivenNullServices()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddPersistenceConfiguration(configuration));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            services.AddPersistenceConfiguration(null!));
        Assert.Equal("configuration", ex.ParamName);
    }
}
