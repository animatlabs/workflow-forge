using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkflowForge.Extensions.Audit;
using WorkflowForge.Extensions.Audit.Options;
using WorkflowForge.Extensions.Persistence;
using WorkflowForge.Extensions.Persistence.Recovery;
using WorkflowForge.Extensions.Persistence.Recovery.Options;
using WorkflowForge.Extensions.Resilience.Polly;
using WorkflowForge.Extensions.Resilience.Polly.Options;
using WorkflowForge.Extensions.Validation;
using WorkflowForge.Extensions.Validation.Options;
using WorkflowForge.Options;

namespace WorkflowForge.Extensions.DependencyInjection.Tests
{
    public class ConfigurationBindingTests
    {
        [Fact]
        public void AddAuditConfiguration_WithDefaultSection_ShouldBindCorrectly()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["WorkflowForge:Extensions:Audit:Enabled"] = "false",
                    ["WorkflowForge:Extensions:Audit:DetailLevel"] = "Verbose"
                })
                .Build();

            services.AddAuditConfiguration(config);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<AuditMiddlewareOptions>>().Value;

            Assert.False(options.Enabled);
            Assert.Equal(AuditDetailLevel.Verbose, options.DetailLevel);
        }

        [Fact]
        public void AddAuditConfiguration_WithCustomSection_ShouldBindCorrectly()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MyApp:Audit:Enabled"] = "true"
                })
                .Build();

            services.AddAuditConfiguration(config, "MyApp:Audit");
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<AuditMiddlewareOptions>>().Value;

            Assert.True(options.Enabled);
        }

        [Fact]
        public void AddValidationConfiguration_WithDefaultSection_ShouldBindCorrectly()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["WorkflowForge:Extensions:Validation:Enabled"] = "false",
                    ["WorkflowForge:Extensions:Validation:ThrowOnValidationError"] = "false"
                })
                .Build();

            services.AddValidationConfiguration(config);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<ValidationMiddlewareOptions>>().Value;

            Assert.False(options.Enabled);
            Assert.False(options.ThrowOnValidationError);
        }

        [Fact]
        public void AddPersistenceConfiguration_WithDefaultSection_ShouldBindCorrectly()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["WorkflowForge:Extensions:Persistence:Enabled"] = "false",
                    ["WorkflowForge:Extensions:Persistence:MaxVersions"] = "5"
                })
                .Build();

            services.AddPersistenceConfiguration(config);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;

            Assert.False(options.Enabled);
            Assert.Equal(5, options.MaxVersions);
        }

        [Fact]
        public void AddRecoveryConfiguration_WithDefaultSection_ShouldBindCorrectly()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["WorkflowForge:Extensions:Recovery:Enabled"] = "false",
                    ["WorkflowForge:Extensions:Recovery:MaxRetryAttempts"] = "5"
                })
                .Build();

            services.AddRecoveryConfiguration(config);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<RecoveryMiddlewareOptions>>().Value;

            Assert.False(options.Enabled);
            Assert.Equal(5, options.MaxRetryAttempts);
        }

        [Fact]
        public void AddWorkflowForgePolly_WithDefaultSection_ShouldBindCorrectly()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["WorkflowForge:Extensions:Polly:Enabled"] = "false",
                    ["WorkflowForge:Extensions:Polly:Retry:IsEnabled"] = "true",
                    ["WorkflowForge:Extensions:Polly:Retry:MaxRetryAttempts"] = "5"
                })
                .Build();

            services.AddWorkflowForgePolly(config, PollyMiddlewareOptions.DefaultSectionName);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<PollyMiddlewareOptions>();

            Assert.False(options.Enabled);
            Assert.True(options.Retry.IsEnabled);
            Assert.Equal(5, options.Retry.MaxRetryAttempts);
        }

        [Fact]
        public void WorkflowForgeOptionsValidator_WithInvalidOptions_ReturnsFailure()
        {
            var validator = new WorkflowForgeOptionsValidator();
            var options = new WorkflowForgeOptions
            {
                MaxConcurrentWorkflows = -1
            };

            var result = validator.Validate(null, options);

            Assert.False(result.Succeeded);
            Assert.NotEmpty(result.Failures!);
        }

        [Fact]
        public void AddWorkflowForge_RegistersOptionsValidator()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            services.AddWorkflowForge(config);
            var provider = services.BuildServiceProvider();
            var validators = provider.GetServices<IValidateOptions<WorkflowForgeOptions>>();

            Assert.Contains(validators, v => v is WorkflowForgeOptionsValidator);
        }
    }
}
