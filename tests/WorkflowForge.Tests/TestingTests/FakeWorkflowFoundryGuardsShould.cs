using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;
using WorkflowForge.Testing;

namespace WorkflowForge.Tests.TestingTests;

public class FakeWorkflowFoundryGuardsShould
{
    [Fact]
    public void ThrowArgumentNullException_GivenNullAddOperation()
    {
        using var foundry = new FakeWorkflowFoundry();

        Assert.Throws<ArgumentNullException>(() => foundry.AddOperation(null!));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullAddMiddleware()
    {
        using var foundry = new FakeWorkflowFoundry();

        Assert.Throws<ArgumentNullException>(() => foundry.AddMiddleware(null!));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullAddMiddlewares()
    {
        using var foundry = new FakeWorkflowFoundry();

        Assert.Throws<ArgumentNullException>(() => foundry.AddMiddlewares(null!));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullReplaceOperations()
    {
        using var foundry = new FakeWorkflowFoundry();

        Assert.Throws<ArgumentNullException>(() => foundry.ReplaceOperations(null!));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullTrackExecution()
    {
        using var foundry = new FakeWorkflowFoundry();

        Assert.Throws<ArgumentNullException>(() => foundry.TrackExecution(null!));
    }

    [Fact]
    public void ReplaceOperations_GivenNewSet()
    {
        using var foundry = new FakeWorkflowFoundry();
        foundry.AddOperation(new NoOpOperation("Initial"));
        var replacement = new[]
        {
            new NoOpOperation("Replacement-1"),
            new NoOpOperation("Replacement-2")
        };

        foundry.ReplaceOperations(replacement);

        Assert.Equal(2, foundry.Operations.Count);
        Assert.Equal("Replacement-1", foundry.Operations[0].Name);
        Assert.Equal("Replacement-2", foundry.Operations[1].Name);
    }

    [Fact]
    public void AddMiddlewares_GivenCollection()
    {
        using var foundry = new FakeWorkflowFoundry();
        var middlewares = new IWorkflowOperationMiddleware[]
        {
            new PassThroughMiddleware(),
            new PassThroughMiddleware()
        };

        foundry.AddMiddlewares(middlewares);

        Assert.Equal(2, foundry.Middlewares.Count);
    }

    [Fact]
    public async Task ThrowOperationCanceledException_GivenCanceledToken()
    {
        using var foundry = new FakeWorkflowFoundry();
        foundry.AddOperation(new NoOpOperation("Cancelable"));
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => foundry.ForgeAsync(cancellationSource.Token));
        Assert.False(foundry.IsFrozen);
    }

    [Fact]
    public void ThrowObjectDisposedException_GivenDisposedInstance()
    {
        var foundry = new FakeWorkflowFoundry();
        foundry.Dispose();

        Assert.Throws<ObjectDisposedException>(() => foundry.SetCurrentWorkflow(null));
    }

    private sealed class NoOpOperation : WorkflowOperationBase
    {
        public NoOpOperation(string name)
        {
            Name = name;
        }

        public override string Name { get; }

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken ct)
        {
            return Task.FromResult(inputData);
        }
    }

    private sealed class PassThroughMiddleware : IWorkflowOperationMiddleware
    {
        public Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, object? inputData, Func<CancellationToken, Task<object?>> next, CancellationToken cancellationToken = default)
        {
            return next(cancellationToken);
        }
    }
}
