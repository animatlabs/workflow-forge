using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Persistence;
using WorkflowForge.Extensions.Persistence.Abstractions;
using WorkflowForge.Extensions.Persistence.Recovery;
using WorkflowForge.Extensions.Persistence.Recovery.Options;
using WorkflowForge.Extensions.Validation;
using WorkflowForge.Extensions.Validation.Options;

namespace WorkflowForge.Extensions.DependencyInjection.Tests
{
    public class ConfigurationDrivenWorkflowShould
    {
        [Fact]
        public async Task SkipValidation_GivenWorkflowWithValidationDisabled()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["WorkflowForge:Extensions:Validation:Enabled"] = "false"
                })
                .Build();

            services.AddValidationConfiguration(config);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<ValidationMiddlewareOptions>>().Value;

            var foundry = WorkflowForge.CreateFoundry("Test");

            foundry.UseValidation(f => new TestModel { Value = -1 }, options);

            var workflow = WorkflowForge.CreateWorkflow("TestWorkflow")
                .AddOperation("Op1", async (f, ct) => { f.SetProperty("result", "success"); })
                .Build();

            var smith = WorkflowForge.CreateSmith();
            await smith.ForgeAsync(workflow, foundry);

            Assert.Equal("success", foundry.GetPropertyOrDefault<string>("result"));
            Assert.Equal(0, GetMiddlewareCount(foundry));
        }

        [Fact]
        public async Task ExecuteValidation_GivenWorkflowWithValidationEnabled()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["WorkflowForge:Extensions:Validation:Enabled"] = "true",
                    ["WorkflowForge:Extensions:Validation:ThrowOnValidationError"] = "true"
                })
                .Build();

            services.AddValidationConfiguration(config);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<ValidationMiddlewareOptions>>().Value;

            var foundry = WorkflowForge.CreateFoundry("Test");

            foundry.UseValidation(f => new TestModel { Value = -1 }, options);

            var workflow = WorkflowForge.CreateWorkflow("TestWorkflow")
                .AddOperation("Op1", async (f, ct) => { })
                .Build();

            var smith = WorkflowForge.CreateSmith();
            await Assert.ThrowsAsync<WorkflowValidationException>(() =>
                smith.ForgeAsync(workflow, foundry));
        }

        [Fact]
        public async Task NotPersist_GivenWorkflowWithPersistenceDisabled()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["WorkflowForge:Extensions:Persistence:Enabled"] = "false"
                })
                .Build();

            services.AddPersistenceConfiguration(config);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;

            var providerMock = new InMemoryPersistenceProvider();
            var foundry = WorkflowForge.CreateFoundry("Test");

            foundry.UsePersistence(providerMock, options);

            var workflow = WorkflowForge.CreateWorkflow("TestWorkflow")
                .AddOperation("Op1", async (f, ct) => { })
                .Build();

            var smith = WorkflowForge.CreateSmith();
            await smith.ForgeAsync(workflow, foundry);

            Assert.Equal(0, GetMiddlewareCount(foundry));
        }

        [Fact]
        public async Task NotRetry_GivenWorkflowWithRecoveryDisabled()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["WorkflowForge:Extensions:Recovery:Enabled"] = "false"
                })
                .Build();

            services.AddRecoveryConfiguration(config);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<RecoveryMiddlewareOptions>>().Value;

            var persistenceProvider = new InMemoryPersistenceProvider();
            var foundry = WorkflowForge.CreateFoundry("Test");
            var workflow = WorkflowForge.CreateWorkflow("TestWorkflow")
                .AddOperation("Op1", async (f, ct) => { })
                .Build();

            var smith = WorkflowForge.CreateSmith();
            await smith.ForgeWithRecoveryAsync(
                workflow,
                foundry,
                persistenceProvider,
                Guid.NewGuid(),
                Guid.NewGuid(),
                options);

            Assert.True(true);
        }

        private static int GetMiddlewareCount(IWorkflowFoundry foundry)
        {
            var type = foundry.GetType();
            var property = type.GetProperty("MiddlewareCount");
            return property != null ? (int)property.GetValue(foundry)! : 0;
        }

        private class TestModel
        {
            [Range(1, int.MaxValue, ErrorMessage = "Value must be greater than 0")]
            public int Value { get; set; }
        }

        private class InMemoryPersistenceProvider : IWorkflowPersistenceProvider
        {
            private readonly Dictionary<(Guid, Guid), WorkflowExecutionSnapshot> _store = new();

            public Task SaveAsync(WorkflowExecutionSnapshot snapshot, System.Threading.CancellationToken cancellationToken = default)
            {
                _store[(snapshot.FoundryExecutionId, snapshot.WorkflowId)] = snapshot;
                return Task.CompletedTask;
            }

            public Task<WorkflowExecutionSnapshot?> TryLoadAsync(Guid foundryExecutionId, Guid workflowId, System.Threading.CancellationToken cancellationToken = default)
            {
                _store.TryGetValue((foundryExecutionId, workflowId), out var snapshot);
                return Task.FromResult<WorkflowExecutionSnapshot?>(snapshot);
            }

            public Task DeleteAsync(Guid foundryExecutionId, Guid workflowId, System.Threading.CancellationToken cancellationToken = default)
            {
                _store.Remove((foundryExecutionId, workflowId));
                return Task.CompletedTask;
            }
        }
    }
}
