using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkflowForge.Extensions.Persistence.Recovery;
using WorkflowForge.Extensions.Persistence.Recovery.Options;
using Xunit;

namespace WorkflowForge.Extensions.Persistence.Tests.Recovery;

public class RecoveryServiceCollectionExtensionsShould
{
    [Fact]
    public void ThrowArgumentNullException_GivenNullServices()
    {
        var configuration = new ConfigurationBuilder().Build();
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() =>
            services.AddRecoveryConfiguration(configuration));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullConfiguration()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddRecoveryConfiguration(null!));
    }

    [Fact]
    public void RegisterOptionsFromDefaultSection_GivenConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["WorkflowForge:Extensions:Recovery:Enabled"] = "true",
            ["WorkflowForge:Extensions:Recovery:MaxRetryAttempts"] = "6",
            ["WorkflowForge:Extensions:Recovery:BaseDelay"] = "00:00:03",
            ["WorkflowForge:Extensions:Recovery:UseExponentialBackoff"] = "false",
            ["WorkflowForge:Extensions:Recovery:AttemptResume"] = "true",
            ["WorkflowForge:Extensions:Recovery:LogRecoveryAttempts"] = "false"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddRecoveryConfiguration(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RecoveryMiddlewareOptions>>().Value;

        Assert.True(options.Enabled);
        Assert.Equal(6, options.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(3), options.BaseDelay);
        Assert.False(options.UseExponentialBackoff);
        Assert.True(options.AttemptResume);
        Assert.False(options.LogRecoveryAttempts);
    }

    [Fact]
    public void RegisterOptionsFromCustomSection_GivenConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["MyRecovery:Enabled"] = "false",
            ["MyRecovery:MaxRetryAttempts"] = "2",
            ["MyRecovery:BaseDelay"] = "00:00:01"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddRecoveryConfiguration(configuration, "MyRecovery");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RecoveryMiddlewareOptions>>().Value;

        Assert.False(options.Enabled);
        Assert.Equal(2, options.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), options.BaseDelay);
    }
}
