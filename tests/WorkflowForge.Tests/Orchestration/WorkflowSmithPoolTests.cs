using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Tests.Orchestration
{
    public class WorkflowSmithPoolShould : IDisposable
    {
        private readonly string _uniqueTestId;

        public WorkflowSmithPoolShould()
        {
            _uniqueTestId = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}";
        }

        public void Dispose() { }

        [Fact]
        public async Task ReusePooledFoundry_GivenConsecutiveForgeAsyncCalls()
        {
            using var smith = WorkflowForge.CreateSmith();
            var workflow1 = CreateNoOpWorkflow($"Pool-Reuse-1-{_uniqueTestId}");
            var workflow2 = CreateNoOpWorkflow($"Pool-Reuse-2-{_uniqueTestId}");

            var ex = await Record.ExceptionAsync(async () =>
            {
                await smith.ForgeAsync(workflow1);
                await smith.ForgeAsync(workflow2);
            });

            Assert.Null(ex);
        }

        [Fact]
        public async Task DrainPoolWithoutException_GivenDisposeAfterForgeAsync()
        {
            var smith = WorkflowForge.CreateSmith();
            var workflow = CreateNoOpWorkflow($"Pool-Drain-{_uniqueTestId}");

            await smith.ForgeAsync(workflow);

            var ex = Record.Exception(() => smith.Dispose());

            Assert.Null(ex);
        }

        [Fact]
        public async Task CompleteWithinPoolBounds_GivenManyParallelExecutions()
        {
            using var smith = WorkflowForge.CreateSmith();
            var concurrency = Environment.ProcessorCount * 2 + 5;

            var tasks = Enumerable.Range(0, concurrency)
                .Select(i => smith.ForgeAsync(CreateNoOpWorkflow($"Pool-Max-{_uniqueTestId}-{i}")))
                .ToArray();

            var ex = await Record.ExceptionAsync(() => Task.WhenAll(tasks));

            Assert.Null(ex);
        }

        [Fact]
        public async Task NotThrowUnhandledException_GivenConcurrentForgeAndDisposeRace()
        {
            var smith = WorkflowForge.CreateSmith();
            var cts = new CancellationTokenSource();
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
        public async Task CompleteAllSuccessfully_GivenSequentialBatch()
        {
            using var smith = WorkflowForge.CreateSmith();

            for (var i = 0; i < 20; i++)
            {
                var ex = await Record.ExceptionAsync(() =>
                    smith.ForgeAsync(CreateNoOpWorkflow($"Batch-{_uniqueTestId}-{i}")));
                Assert.Null(ex);
            }
        }

        [Fact]
        public async Task NotThrow_GivenMultipleDisposeCalls()
        {
            var smith = WorkflowForge.CreateSmith();
            await smith.ForgeAsync(CreateNoOpWorkflow($"Multi-Dispose-{_uniqueTestId}"));

            var ex1 = Record.Exception(() => smith.Dispose());
            var ex2 = Record.Exception(() => smith.Dispose());

            Assert.Null(ex1);
            Assert.Null(ex2);
        }

        private static Workflow CreateNoOpWorkflow(string name)
        {
            return new Workflow(
                name,
                "Pool test workflow",
                "1.0.0",
                new List<IWorkflowOperation> { new NoOpOperation() },
                new Dictionary<string, object?>());
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

            public void Dispose() { }
        }
    }
}
