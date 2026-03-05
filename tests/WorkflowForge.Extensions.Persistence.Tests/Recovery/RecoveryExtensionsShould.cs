using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Events;
using WorkflowForge.Extensions.Persistence.Abstractions;
using WorkflowForge.Extensions.Persistence.Recovery;
using WorkflowForge.Extensions.Persistence.Recovery.Options;
using Xunit;

namespace WorkflowForge.Extensions.Persistence.Tests.Recovery;

public class RecoveryExtensionsShould
{
    [Fact]
    public async Task ThrowArgumentNullException_GivenNullSmith()
    {
        var workflow = WorkflowForge.CreateWorkflow("Recovery-NullSmith")
            .AddOperation("NoOp", (foundry, cancellationToken) => Task.CompletedTask)
            .Build();
        using var foundry = WorkflowForge.CreateFoundry("Recovery-NullSmith");
        var provider = new CountingPersistenceProvider();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            RecoveryExtensions.ForgeWithRecoveryAsync(
                smith: null!,
                workflow,
                foundry,
                provider,
                Guid.NewGuid(),
                Guid.NewGuid()));
    }

    [Fact]
    public async Task ThrowArgumentNullException_GivenNullWorkflow()
    {
        var smith = new CountingWorkflowSmith();
        using var foundry = WorkflowForge.CreateFoundry("Recovery-NullWorkflow");
        var provider = new CountingPersistenceProvider();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            smith.ForgeWithRecoveryAsync(
                workflow: null!,
                foundry,
                provider,
                Guid.NewGuid(),
                Guid.NewGuid()));
    }

    [Fact]
    public async Task ThrowArgumentNullException_GivenNullFoundry()
    {
        var smith = new CountingWorkflowSmith();
        var workflow = WorkflowForge.CreateWorkflow("Recovery-NullFoundry")
            .AddOperation("NoOp", (foundry, cancellationToken) => Task.CompletedTask)
            .Build();
        var provider = new CountingPersistenceProvider();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            smith.ForgeWithRecoveryAsync(
                workflow,
                foundry: null!,
                provider,
                Guid.NewGuid(),
                Guid.NewGuid()));
    }

    [Fact]
    public async Task ThrowArgumentNullException_GivenNullProvider()
    {
        var smith = new CountingWorkflowSmith();
        var workflow = WorkflowForge.CreateWorkflow("Recovery-NullProvider")
            .AddOperation("NoOp", (foundry, cancellationToken) => Task.CompletedTask)
            .Build();
        using var foundry = WorkflowForge.CreateFoundry("Recovery-NullProvider");

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            smith.ForgeWithRecoveryAsync(
                workflow,
                foundry,
                provider: null!,
                Guid.NewGuid(),
                Guid.NewGuid()));
    }

    [Fact]
    public async Task ExecuteWithoutRecovery_GivenRecoveryDisabled()
    {
        var smith = new CountingWorkflowSmith();
        var workflow = WorkflowForge.CreateWorkflow("Recovery-Disabled")
            .AddOperation("NoOp", (foundry, cancellationToken) => Task.CompletedTask)
            .Build();
        using var foundry = WorkflowForge.CreateFoundry("Recovery-Disabled");
        var provider = new CountingPersistenceProvider();

        await smith.ForgeWithRecoveryAsync(
            workflow,
            foundry,
            provider,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new RecoveryMiddlewareOptions { Enabled = false });

        Assert.Equal(1, smith.ForgeWithFoundryCalls);
        Assert.Equal(0, provider.TryLoadCalls);
    }

    [Fact]
    public async Task ExecuteFreshWorkflow_GivenNoSnapshot()
    {
        var smith = new CountingWorkflowSmith();
        var workflow = WorkflowForge.CreateWorkflow("Recovery-NoSnapshot")
            .AddOperation("Mark", (foundry, cancellationToken) =>
            {
                foundry.SetProperty("executed", true);
                return Task.CompletedTask;
            })
            .Build();
        using var foundry = WorkflowForge.CreateFoundry("Recovery-NoSnapshot");
        var provider = new CountingPersistenceProvider();

        await smith.ForgeWithRecoveryAsync(
            workflow,
            foundry,
            provider,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new RecoveryMiddlewareOptions { Enabled = true, MaxRetryAttempts = 2 });

        Assert.Equal(1, provider.TryLoadCalls);
        Assert.Equal(1, smith.ForgeWithFoundryCalls);
    }

    [Fact]
    public async Task SkipFreshExecution_GivenSnapshotResumeSucceeds()
    {
        var foundryKey = Guid.NewGuid();
        var workflowKey = Guid.NewGuid();
        var snapshot = new WorkflowExecutionSnapshot
        {
            FoundryExecutionId = foundryKey,
            WorkflowId = workflowKey,
            WorkflowName = "Recovery-Resume",
            NextOperationIndex = 0,
            Properties = new Dictionary<string, object?>
            {
                ["seed"] = 7
            }
        };

        var provider = new CountingPersistenceProvider(snapshot);
        var smith = new CountingWorkflowSmith
        {
            ThrowOnForgeWithFoundry = true
        };

        var workflow = WorkflowForge.CreateWorkflow("Recovery-Resume")
            .AddOperation("ResumeOp", (foundry, cancellationToken) =>
            {
                var seed = foundry.GetPropertyOrDefault<int>("seed");
                foundry.SetProperty("resumed", seed == 7);
                return Task.CompletedTask;
            })
            .Build();

        using var foundry = WorkflowForge.CreateFoundry("Recovery-Resume");

        await smith.ForgeWithRecoveryAsync(
            workflow,
            foundry,
            provider,
            foundryKey,
            workflowKey,
            new RecoveryMiddlewareOptions { Enabled = true, MaxRetryAttempts = 2 });

        Assert.Equal(0, smith.ForgeWithFoundryCalls);
        Assert.Equal(2, provider.TryLoadCalls);
    }

    [Fact]
    public async Task RetryFreshExecutionUntilSuccess_GivenTransientFailure()
    {
        var smith = new CountingWorkflowSmith
        {
            FailuresBeforeSuccess = 2
        };
        var workflow = WorkflowForge.CreateWorkflow("Recovery-RetryFresh")
            .AddOperation("NoOp", (foundry, cancellationToken) => Task.CompletedTask)
            .Build();
        using var foundry = WorkflowForge.CreateFoundry("Recovery-RetryFresh");
        var provider = new CountingPersistenceProvider();

        await smith.ForgeWithRecoveryAsync(
            workflow,
            foundry,
            provider,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new RecoveryMiddlewareOptions
            {
                Enabled = true,
                MaxRetryAttempts = 3,
                BaseDelay = TimeSpan.FromMilliseconds(1),
                UseExponentialBackoff = true
            });

        Assert.Equal(3, smith.ForgeWithFoundryCalls);
    }

    [Fact]
    public void ReturnBaseDelay_GivenBackoffDisabled()
    {
        var options = new RecoveryMiddlewareOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(15),
            UseExponentialBackoff = false
        };

        var method = typeof(RecoveryExtensions).GetMethod("GetRetryDelay", BindingFlags.NonPublic | BindingFlags.Static);
        var delay = (TimeSpan)method!.Invoke(null, new object[] { options, 3 })!;

        Assert.Equal(TimeSpan.FromMilliseconds(15), delay);
    }

    [Fact]
    public void ReturnExponentialDelay_GivenBackoffEnabled()
    {
        var options = new RecoveryMiddlewareOptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(20),
            UseExponentialBackoff = true
        };

        var method = typeof(RecoveryExtensions).GetMethod("GetRetryDelay", BindingFlags.NonPublic | BindingFlags.Static);
        var delaySecondAttempt = (TimeSpan)method!.Invoke(null, new object[] { options, 2 })!;
        var delayThirdAttempt = (TimeSpan)method!.Invoke(null, new object[] { options, 3 })!;

        Assert.Equal(TimeSpan.FromMilliseconds(40), delaySecondAttempt);
        Assert.Equal(TimeSpan.FromMilliseconds(80), delayThirdAttempt);
    }

    private sealed class CountingPersistenceProvider : IWorkflowPersistenceProvider
    {
        private readonly ConcurrentDictionary<(Guid FoundryId, Guid WorkflowId), WorkflowExecutionSnapshot> _snapshots = new();

        public CountingPersistenceProvider(params WorkflowExecutionSnapshot[] snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                _snapshots[(snapshot.FoundryExecutionId, snapshot.WorkflowId)] = snapshot;
            }
        }

        public int TryLoadCalls { get; private set; }

        public Task SaveAsync(WorkflowExecutionSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshots[(snapshot.FoundryExecutionId, snapshot.WorkflowId)] = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkflowExecutionSnapshot?> TryLoadAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken cancellationToken = default)
        {
            TryLoadCalls++;
            _snapshots.TryGetValue((foundryExecutionId, workflowId), out var snapshot);
            return Task.FromResult<WorkflowExecutionSnapshot?>(snapshot);
        }

        public Task DeleteAsync(Guid foundryExecutionId, Guid workflowId, CancellationToken cancellationToken = default)
        {
            _snapshots.TryRemove((foundryExecutionId, workflowId), out _);
            return Task.CompletedTask;
        }
    }

    private sealed class CountingWorkflowSmith : IWorkflowSmith
    {
        public event EventHandler<WorkflowStartedEventArgs>? WorkflowStarted;

        public event EventHandler<WorkflowCompletedEventArgs>? WorkflowCompleted;

        public event EventHandler<WorkflowFailedEventArgs>? WorkflowFailed;

        public event EventHandler<CompensationTriggeredEventArgs>? CompensationTriggered;

        public event EventHandler<CompensationCompletedEventArgs>? CompensationCompleted;

        public event EventHandler<OperationRestoreStartedEventArgs>? OperationRestoreStarted;

        public event EventHandler<OperationRestoreCompletedEventArgs>? OperationRestoreCompleted;

        public event EventHandler<OperationRestoreFailedEventArgs>? OperationRestoreFailed;

        public int ForgeWithFoundryCalls { get; private set; }
        public int FailuresBeforeSuccess { get; set; }
        public bool ThrowOnForgeWithFoundry { get; set; }

        public Task ForgeAsync(IWorkflow workflow, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ForgeAsync(IWorkflow workflow, ConcurrentDictionary<string, object?> data, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ForgeAsync(IWorkflow workflow, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            ForgeWithFoundryCalls++;
            if (ThrowOnForgeWithFoundry)
            {
                throw new InvalidOperationException("Fresh execution should not run in this scenario");
            }

            if (ForgeWithFoundryCalls <= FailuresBeforeSuccess)
            {
                throw new InvalidOperationException("Transient failure from test smith");
            }

            return Task.CompletedTask;
        }

        public IWorkflowFoundry CreateFoundry(IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null)
        {
            return WorkflowForge.CreateFoundry("Test-CreateFoundry", logger);
        }

        public IWorkflowFoundry CreateFoundryFor(IWorkflow workflow, IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null)
        {
            return WorkflowForge.CreateFoundry("Test-CreateFoundryFor", logger);
        }

        public IWorkflowFoundry CreateFoundryWithData(ConcurrentDictionary<string, object?> data, IWorkflowForgeLogger? logger = null, IServiceProvider? serviceProvider = null)
        {
            return WorkflowForge.CreateFoundry("Test-CreateFoundryWithData", logger, data);
        }

        public void AddWorkflowMiddleware(IWorkflowMiddleware middleware)
        {
        }

        public void Dispose()
        {
        }
    }
}
