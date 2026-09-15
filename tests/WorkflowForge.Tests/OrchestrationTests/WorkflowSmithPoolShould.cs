using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Tests.OrchestrationTests
{
    public class WorkflowSmithPoolShould : IDisposable
    {
        private readonly string _uniqueTestId;

        public WorkflowSmithPoolShould()
        {
            _uniqueTestId = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}";
        }

        public void Dispose()
        { }

        [Fact]
        public async Task ReuseTheSameFoundryInstance_GivenConsecutiveForgeAsyncCalls()
        {
            using var smith = WorkflowForge.CreateSmith();
            var first = new RecordingOperation();
            var second = new RecordingOperation();

            await smith.ForgeAsync(CreateWorkflow($"Pool-Reuse-1-{_uniqueTestId}", first));
            await smith.ForgeAsync(CreateWorkflow($"Pool-Reuse-2-{_uniqueTestId}", second));

            Assert.NotNull(first.SeenFoundry);
            Assert.NotNull(second.SeenFoundry);
            Assert.Same(first.SeenFoundry, second.SeenFoundry);
            Assert.NotEqual(first.SeenExecutionId, second.SeenExecutionId);
        }

        [Fact]
        public async Task StartEachRunWithEmptyProperties_GivenAPooledFoundry()
        {
            using var smith = WorkflowForge.CreateSmith();
            var first = new RecordingOperation(foundry => foundry.Properties["carryover"] = "leaked");
            var second = new RecordingOperation();

            await smith.ForgeAsync(CreateWorkflow($"Pool-Clean-1-{_uniqueTestId}", first));
            await smith.ForgeAsync(CreateWorkflow($"Pool-Clean-2-{_uniqueTestId}", second));

            Assert.False(second.SawCarryover);
        }

        [Fact]
        public async Task NotLeakStalePropertiesAcrossPooledRuns_WhenSmallerWorkflowFails()
        {
            using var smith = WorkflowForge.CreateSmith();

            // First run: a 3-operation workflow that completes, leaving LastCompletedIndex = 2 on the
            // pooled foundry's Properties.
            await smith.ForgeAsync(CreateNoOpWorkflow($"Leak-Big-{_uniqueTestId}", 3));

            // Reuse the pooled foundry for a 1-operation workflow whose only op fails. If stale
            // Properties leaked, compensation would walk from the stale index (2) and hit
            // operations[2] on a 1-element list, throwing ArgumentOutOfRangeException and masking
            // the operation's real exception.
            var failing = new Workflow(
                $"Leak-Small-{_uniqueTestId}",
                "fails on its only operation",
                "1.0.0",
                new List<IWorkflowOperation> { new ThrowingOperation() },
                new Dictionary<string, object?>());

            var ex = await Record.ExceptionAsync(() => smith.ForgeAsync(failing));

            Assert.IsType<InvalidOperationException>(ex);
        }

        [Fact]
        public async Task RejectFurtherWork_GivenDisposeAfterForgeAsync()
        {
            var smith = WorkflowForge.CreateSmith();
            var workflow = CreateNoOpWorkflow($"Pool-Drain-{_uniqueTestId}");

            await smith.ForgeAsync(workflow);
            smith.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => smith.ForgeAsync(workflow));
        }

        [Fact]
        public async Task RunEveryWorkflowOnItsOwnFoundry_GivenManyParallelExecutions()
        {
            using var smith = WorkflowForge.CreateSmith();
            var concurrency = Environment.ProcessorCount * 2 + 5;
            var foundries = new ConcurrentBag<IWorkflowFoundry>();

            var operations = Enumerable.Range(0, concurrency)
                .Select(_ => new RecordingOperation(foundry => foundries.Add(foundry)))
                .ToArray();

            var tasks = operations
                .Select((op, i) => smith.ForgeAsync(CreateWorkflow($"Pool-Max-{_uniqueTestId}-{i}", op)))
                .ToArray();

            await Task.WhenAll(tasks);

            // Every concurrent run must have had its own foundry; the pool may only reuse
            // instances after a run returns them.
            Assert.Equal(concurrency, foundries.Count);
            Assert.Equal(concurrency, operations.Select(o => o.SeenExecutionId).Distinct().Count());
        }

        [Fact]
        public async Task NotThrowUnhandledException_GivenConcurrentForgeAndDisposeRace()
        {
            var smith = WorkflowForge.CreateSmith();
            var exceptions = new ConcurrentBag<Exception>();

            var forgeTask = Task.Run(async () =>
            {
                try
                {
                    for (var i = 0; i < 10; i++)
                    {
                        await smith.ForgeAsync(CreateNoOpWorkflow($"Race-{_uniqueTestId}-{i}"));
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Expected when dispose races with forge
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            await Task.Delay(5);

            var disposeEx = Record.Exception(() => smith.Dispose());
            await forgeTask;

            Assert.Null(disposeEx);
            Assert.Empty(exceptions);
        }

        [Fact]
        public async Task ExecuteEveryOperation_GivenSequentialBatch()
        {
            using var smith = WorkflowForge.CreateSmith();
            var operations = new List<RecordingOperation>();

            for (var i = 0; i < 20; i++)
            {
                var op = new RecordingOperation();
                operations.Add(op);
                await smith.ForgeAsync(CreateWorkflow($"Batch-{_uniqueTestId}-{i}", op));
            }

            Assert.All(operations, o => Assert.Equal(1, o.Invocations));
            Assert.Equal(20, operations.Select(o => o.SeenExecutionId).Distinct().Count());
        }

        [Fact]
        public async Task StayDisposed_GivenMultipleDisposeCalls()
        {
            var smith = WorkflowForge.CreateSmith();
            var workflow = CreateNoOpWorkflow($"Multi-Dispose-{_uniqueTestId}");
            await smith.ForgeAsync(workflow);

            smith.Dispose();
            smith.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => smith.ForgeAsync(workflow));
        }

        private static Workflow CreateWorkflow(string name, IWorkflowOperation operation)
        {
            return new Workflow(
                name,
                "Pool test workflow",
                "1.0.0",
                new List<IWorkflowOperation> { operation },
                new Dictionary<string, object?>());
        }

        private sealed class RecordingOperation : IWorkflowOperation
        {
            private readonly Action<IWorkflowFoundry>? _onForge;
            private int _invocations;

            public RecordingOperation(Action<IWorkflowFoundry>? onForge = null)
            {
                _onForge = onForge;
            }

            public Guid Id { get; } = Guid.NewGuid();

            public string Name => "Recording";

            public int Invocations => Volatile.Read(ref _invocations);

            public IWorkflowFoundry? SeenFoundry { get; private set; }

            public Guid SeenExecutionId { get; private set; }

            public bool SawCarryover { get; private set; }

            public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref _invocations);
                SeenFoundry = foundry;
                SeenExecutionId = foundry.ExecutionId;
                SawCarryover = foundry.Properties.ContainsKey("carryover");
                _onForge?.Invoke(foundry);
                return Task.FromResult<object?>(null);
            }

            public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public void Dispose()
            { }
        }

        private static Workflow CreateNoOpWorkflow(string name, int operationCount = 1)
        {
            var operations = new List<IWorkflowOperation>();
            for (var i = 0; i < operationCount; i++)
            {
                operations.Add(new NoOpOperation());
            }

            return new Workflow(
                name,
                "Pool test workflow",
                "1.0.0",
                operations,
                new Dictionary<string, object?>());
        }

        private sealed class ThrowingOperation : IWorkflowOperation
        {
            public Guid Id { get; } = Guid.NewGuid();
            public string Name => "Throwing";
            public string? Description => null;

            public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("Intentional failure for pool-reuse test.");

            public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public void Dispose()
            { }
        }

        private sealed class NoOpOperation : IWorkflowOperation
        {
            public Guid Id { get; } = Guid.NewGuid();
            public string Name => "NoOp";
            public string? Description => null;

            public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
                => Task.FromResult<object?>(null);

            public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public void Dispose()
            { }
        }
    }
}
