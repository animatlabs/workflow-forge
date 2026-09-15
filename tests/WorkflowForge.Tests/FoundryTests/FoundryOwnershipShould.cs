using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Tests.FoundryTests;

public class FoundryOwnershipShould
{
    [Fact]
    public async Task LeaveCallerOperationsUndisposed_WhenSmithCompletes()
    {
        var operation = new DisposableOperation();
        var workflow = WorkflowForge.CreateWorkflow("OwnershipOperations")
            .AddOperation(operation)
            .Build();

        using (var smith = WorkflowForge.CreateSmith())
        {
            await smith.ForgeAsync(workflow);
        }

        Assert.False(operation.Disposed);
    }

    [Fact]
    public async Task LeaveCallerMiddlewareUndisposed_WhenFoundryDisposed()
    {
        var middleware = new DisposableMiddleware();
        var workflow = WorkflowForge.CreateWorkflow("OwnershipMiddleware")
            .AddOperation("noop", (_, _) => Task.CompletedTask)
            .Build();

        using (var foundry = WorkflowForge.CreateFoundry("OwnershipMiddleware"))
        {
            foundry.AddMiddleware(middleware);
            using var smith = WorkflowForge.CreateSmith();
            await smith.ForgeAsync(workflow, foundry);
        }

        Assert.False(middleware.Disposed);
        Assert.Equal(1, middleware.Invocations);
    }

    [Fact]
    public async Task ReuseWorkflowAcrossRuns_GivenScopedSmith()
    {
        var operation = new DisposableOperation();
        var workflow = WorkflowForge.CreateWorkflow("OwnershipReuse")
            .AddOperation(operation)
            .Build();

        using (var first = WorkflowForge.CreateSmith())
        {
            await first.ForgeAsync(workflow);
        }

        using var second = WorkflowForge.CreateSmith();
        await second.ForgeAsync(workflow);

        Assert.Equal(2, operation.Invocations);
    }

    [Fact]
    public async Task PreserveCallerDictionary_WhenForgedWithData()
    {
        var data = new ConcurrentDictionary<string, object?>();
        data["Input"] = "in";

        var workflow = WorkflowForge.CreateWorkflow("OwnershipData")
            .AddOperation("write", (foundry, _) =>
            {
                foundry.Properties["Output"] = "out";
                return Task.CompletedTask;
            })
            .Build();

        using var smith = WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, data);

        Assert.Equal("in", data["Input"]);
        Assert.Equal("out", data["Output"]);
    }

    private sealed class DisposableOperation : IWorkflowOperation
    {
        private int _invocations;

        public Guid Id { get; } = Guid.NewGuid();

        public string Name => "DisposableOperation";

        public int Invocations => _invocations;

        public bool Disposed { get; private set; }

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DisposableOperation));

            Interlocked.Increment(ref _invocations);
            return Task.FromResult(inputData);
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Dispose() => Disposed = true;
    }

    private sealed class DisposableMiddleware : IWorkflowOperationMiddleware, IDisposable
    {
        private int _invocations;

        public int Invocations => _invocations;

        public bool Disposed { get; private set; }

        public Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<CancellationToken, Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _invocations);
            return next(cancellationToken);
        }

        public void Dispose() => Disposed = true;
    }
}
