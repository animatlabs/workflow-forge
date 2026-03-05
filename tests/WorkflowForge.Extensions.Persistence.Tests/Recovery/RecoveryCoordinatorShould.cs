using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;
using WorkflowForge.Extensions.Persistence.Abstractions;
using WorkflowForge.Extensions.Persistence.Recovery;
using WorkflowForge.Extensions.Persistence.Recovery.Options;
using Xunit;

namespace WorkflowForge.Extensions.Persistence.Tests.Recovery;

public class RecoveryCoordinatorShould
{
    [Fact]
    public void ThrowArgumentNullException_GivenNullProvider()
    {
        Assert.Throws<ArgumentNullException>(() => new RecoveryCoordinator(null!));
    }

    [Fact]
    public async Task ReturnImmediately_GivenNoSnapshot()
    {
        var provider = new InMemoryPersistenceProvider();
        var coordinator = new RecoveryCoordinator(provider);
        var foundryFactoryInvoked = false;
        var workflowFactoryInvoked = false;

        await coordinator.ResumeAsync(
            foundryFactory: () =>
            {
                foundryFactoryInvoked = true;
                return WorkflowForge.CreateFoundry("Recovery-NoSnapshot");
            },
            workflowFactory: () =>
            {
                workflowFactoryInvoked = true;
                return WorkflowForge.CreateWorkflow("Recovery-NoSnapshot").Build();
            },
            foundryKey: Guid.NewGuid(),
            workflowKey: Guid.NewGuid());

        Assert.False(foundryFactoryInvoked);
        Assert.False(workflowFactoryInvoked);
    }

    [Fact]
    public async Task RestorePropertiesAndForge_GivenSnapshotExists()
    {
        var provider = new InMemoryPersistenceProvider();
        var foundryKey = Guid.NewGuid();
        var workflowKey = Guid.NewGuid();

        await provider.SaveAsync(new WorkflowExecutionSnapshot
        {
            FoundryExecutionId = foundryKey,
            WorkflowId = workflowKey,
            WorkflowName = "Recovery-Restore",
            NextOperationIndex = 0,
            Properties = new Dictionary<string, object?>
            {
                ["seed"] = 41
            }
        });

        var coordinator = new RecoveryCoordinator(provider);
        IWorkflowFoundry? createdFoundry = null;

        await coordinator.ResumeAsync(
            foundryFactory: () =>
            {
                createdFoundry = WorkflowForge.CreateFoundry("Recovery-Restore");
                return createdFoundry;
            },
            workflowFactory: () => WorkflowForge.CreateWorkflow("Recovery-Restore")
                .AddOperation("Compute", (foundry, cancellationToken) =>
                {
                    var seed = foundry.GetPropertyOrDefault<int>("seed");
                    foundry.SetProperty("result", seed + 1);
                    return Task.CompletedTask;
                })
                .Build(),
            foundryKey: foundryKey,
            workflowKey: workflowKey);

        Assert.NotNull(createdFoundry);
        Assert.Equal(41, createdFoundry!.GetPropertyOrDefault<int>("seed"));
        Assert.Equal(42, createdFoundry.GetPropertyOrDefault<int>("result"));
    }

    [Fact]
    public async Task RetryWithBackoffUntilSuccess_GivenTransientFailure()
    {
        var provider = new InMemoryPersistenceProvider();
        var foundryKey = Guid.NewGuid();
        var workflowKey = Guid.NewGuid();
        var attempts = 0;

        await provider.SaveAsync(new WorkflowExecutionSnapshot
        {
            FoundryExecutionId = foundryKey,
            WorkflowId = workflowKey,
            WorkflowName = "Recovery-Retry",
            NextOperationIndex = 0,
            Properties = new Dictionary<string, object?>()
        });

        var coordinator = new RecoveryCoordinator(
            provider,
            new RecoveryMiddlewareOptions
            {
                MaxRetryAttempts = 3,
                BaseDelay = TimeSpan.FromMilliseconds(1),
                UseExponentialBackoff = true
            });

        await coordinator.ResumeAsync(
            foundryFactory: () => WorkflowForge.CreateFoundry("Recovery-Retry"),
            workflowFactory: () => WorkflowForge.CreateWorkflow("Recovery-Retry")
                .AddOperation("Flaky", (foundry, cancellationToken) =>
                {
                    attempts++;
                    if (attempts < 3)
                    {
                        throw new InvalidOperationException("Transient failure");
                    }

                    foundry.SetProperty("ok", true);
                    return Task.CompletedTask;
                })
                .Build(),
            foundryKey: foundryKey,
            workflowKey: workflowKey);

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ThrowAfterRetriesExhausted_GivenPersistentFailure()
    {
        var provider = new InMemoryPersistenceProvider();
        var foundryKey = Guid.NewGuid();
        var workflowKey = Guid.NewGuid();
        var attempts = 0;

        await provider.SaveAsync(new WorkflowExecutionSnapshot
        {
            FoundryExecutionId = foundryKey,
            WorkflowId = workflowKey,
            WorkflowName = "Recovery-PersistentFailure",
            NextOperationIndex = 0,
            Properties = new Dictionary<string, object?>()
        });

        var coordinator = new RecoveryCoordinator(
            provider,
            new RecoveryMiddlewareOptions
            {
                MaxRetryAttempts = 2,
                BaseDelay = TimeSpan.FromMilliseconds(1),
                UseExponentialBackoff = false
            });

        await Assert.ThrowsAsync<WorkflowOperationException>(() =>
            coordinator.ResumeAsync(
                foundryFactory: () => WorkflowForge.CreateFoundry("Recovery-PersistentFailure"),
                workflowFactory: () => WorkflowForge.CreateWorkflow("Recovery-PersistentFailure")
                    .AddOperation("AlwaysFail", (foundry, cancellationToken) =>
                    {
                        attempts++;
                        throw new InvalidOperationException("Persistent failure");
                    })
                    .Build(),
                foundryKey: foundryKey,
                workflowKey: workflowKey));

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ProcessAllPendingSnapshots_GivenResumeAllAsync()
    {
        var provider = new InMemoryPersistenceProvider();
        var snapshot1 = CreateSnapshot("ResumeAll-1");
        var snapshot2 = CreateSnapshot("ResumeAll-2");

        await provider.SaveAsync(snapshot1);
        await provider.SaveAsync(snapshot2);

        var catalog = new InMemoryRecoveryCatalog(snapshot1, snapshot2);
        var coordinator = new RecoveryCoordinator(provider);
        var completed = 0;

        var resumed = await coordinator.ResumeAllAsync(
            foundryFactory: () => WorkflowForge.CreateFoundry("ResumeAll"),
            workflowFactory: () => WorkflowForge.CreateWorkflow("ResumeAll")
                .AddOperation("Count", (foundry, cancellationToken) =>
                {
                    completed++;
                    return Task.CompletedTask;
                })
                .Build(),
            catalog: catalog);

        Assert.Equal(2, resumed);
        Assert.Equal(2, completed);
    }

    [Fact]
    public async Task ContinueWhenOneResumeFails_GivenResumeAllAsync()
    {
        var provider = new InMemoryPersistenceProvider();
        var healthySnapshot = CreateSnapshot("ResumeAll-Healthy", shouldFail: false);
        var failingSnapshot = CreateSnapshot("ResumeAll-Failing", shouldFail: true);

        await provider.SaveAsync(healthySnapshot);
        await provider.SaveAsync(failingSnapshot);

        var catalog = new InMemoryRecoveryCatalog(healthySnapshot, failingSnapshot);
        var coordinator = new RecoveryCoordinator(provider);

        var resumed = await coordinator.ResumeAllAsync(
            foundryFactory: () => WorkflowForge.CreateFoundry("ResumeAll-Mixed"),
            workflowFactory: () => WorkflowForge.CreateWorkflow("ResumeAll-Mixed")
                .AddOperation("Conditional", (foundry, cancellationToken) =>
                {
                    if (foundry.GetPropertyOrDefault<bool>("shouldFail"))
                    {
                        throw new InvalidOperationException("Expected failure for one snapshot");
                    }

                    foundry.SetProperty("resumed", true);
                    return Task.CompletedTask;
                })
                .Build(),
            catalog: catalog);

        Assert.Equal(1, resumed);
    }

    private static WorkflowExecutionSnapshot CreateSnapshot(string workflowName, bool shouldFail = false)
    {
        return new WorkflowExecutionSnapshot
        {
            FoundryExecutionId = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            WorkflowName = workflowName,
            NextOperationIndex = 0,
            Properties = new Dictionary<string, object?>
            {
                ["shouldFail"] = shouldFail
            }
        };
    }

    private sealed class InMemoryPersistenceProvider : IWorkflowPersistenceProvider
    {
        private readonly ConcurrentDictionary<(Guid FoundryId, Guid WorkflowId), WorkflowExecutionSnapshot> _snapshots = new();

        public Task SaveAsync(WorkflowExecutionSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshots[(snapshot.FoundryExecutionId, snapshot.WorkflowId)] = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkflowExecutionSnapshot?> TryLoadAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken cancellationToken = default)
        {
            _snapshots.TryGetValue((foundryExecutionId, workflowId), out var snapshot);
            return Task.FromResult<WorkflowExecutionSnapshot?>(snapshot);
        }

        public Task DeleteAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken cancellationToken = default)
        {
            _snapshots.TryRemove((foundryExecutionId, workflowId), out _);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRecoveryCatalog : IRecoveryCatalog
    {
        private readonly IReadOnlyList<WorkflowExecutionSnapshot> _snapshots;

        public InMemoryRecoveryCatalog(params WorkflowExecutionSnapshot[] snapshots)
        {
            _snapshots = snapshots;
        }

        public Task<IReadOnlyList<WorkflowExecutionSnapshot>> ListPendingAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_snapshots);
        }
    }
}
