using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Loggers;
using WorkflowForge.Options;

namespace WorkflowForge.Tests.Orchestration
{
    public class WorkflowSmithBranchCoverageShould : IDisposable
    {
        private readonly string _uniqueTestId;

        public WorkflowSmithBranchCoverageShould()
        {
            _uniqueTestId = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}";
        }

        public void Dispose() { }

        [Fact]
        public async Task CompleteSuccessfully_GivenConcurrentDictionaryData()
        {
            using var smith = WorkflowForge.CreateSmith();
            var data = new ConcurrentDictionary<string, object?>();
            data["InputKey"] = "InputValue";

            var capturedValue = string.Empty;
            var workflow = WorkflowForge.CreateWorkflow($"DataOverload-{_uniqueTestId}")
                .AddOperation("ReadData", (foundry, ct) =>
                {
                    if (foundry.Properties.TryGetValue("InputKey", out var val))
                        capturedValue = val?.ToString() ?? string.Empty;
                    return Task.CompletedTask;
                })
                .Build();

            await smith.ForgeAsync(workflow, data);

            Assert.Equal("InputValue", capturedValue);
        }

        [Fact]
        public async Task ThrowArgumentNullException_GivenNullWorkflowWithConcurrentDictionaryData()
        {
            using var smith = WorkflowForge.CreateSmith();
            var data = new ConcurrentDictionary<string, object?>();

            await Assert.ThrowsAsync<ArgumentNullException>(() => smith.ForgeAsync(null!, data));
        }

        [Fact]
        public async Task ThrowArgumentNullException_GivenNullConcurrentDictionaryData()
        {
            using var smith = WorkflowForge.CreateSmith();
            var workflow = WorkflowForge.CreateWorkflow($"NullData-{_uniqueTestId}")
                .AddOperation("NoOp", (_, ct) => Task.CompletedTask)
                .Build();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                smith.ForgeAsync(workflow, (ConcurrentDictionary<string, object?>)null!));
        }

        [Fact]
        public async Task ThrottleConcurrency_GivenConcurrencyLimiterConfigured()
        {
            var options = new WorkflowForgeOptions { MaxConcurrentWorkflows = 2 };
            using var smith = WorkflowForge.CreateSmith(options: options);

            var activeCount = 0;
            var maxObserved = 0;
            var lockObj = new object();

            var tasks = Enumerable.Range(0, 6).Select(i =>
            {
                var workflow = WorkflowForge.CreateWorkflow($"Throttle-{_uniqueTestId}-{i}")
                    .AddOperation($"Op-{i}", async (_, ct) =>
                    {
                        var current = Interlocked.Increment(ref activeCount);
                        lock (lockObj)
                        {
                            if (current > maxObserved)
                                maxObserved = current;
                        }
                        await Task.Delay(50, ct);
                        Interlocked.Decrement(ref activeCount);
                    })
                    .Build();
                return smith.ForgeAsync(workflow);
            }).ToArray();

            await Task.WhenAll(tasks);

            Assert.True(maxObserved <= 2, $"Max concurrent was {maxObserved}, expected <= 2");
        }

        [Fact]
        public async Task ThrowArgumentNullException_GivenNullWorkflowWithExplicitFoundry()
        {
            using var smith = WorkflowForge.CreateSmith();
            using var foundry = smith.CreateFoundry();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                smith.ForgeAsync(null!, foundry));
        }

        [Fact]
        public async Task ThrowArgumentNullException_GivenNullExplicitFoundry()
        {
            using var smith = WorkflowForge.CreateSmith();
            var workflow = WorkflowForge.CreateWorkflow($"NullFoundry-{_uniqueTestId}")
                .AddOperation("NoOp", (_, ct) => Task.CompletedTask)
                .Build();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                smith.ForgeAsync(workflow, (IWorkflowFoundry)null!));
        }

        [Fact]
        public void ReturnIsolatedFoundry_GivenMultipleCreateFoundryCalls()
        {
            using var smith = WorkflowForge.CreateSmith();

            using var foundry1 = smith.CreateFoundry();
            using var foundry2 = smith.CreateFoundry();

            Assert.NotEqual(foundry1.ExecutionId, foundry2.ExecutionId);
            foundry1.Properties["key"] = "val1";
            Assert.False(foundry2.Properties.ContainsKey("key"));
        }

        [Fact]
        public void UseProvidedLogger_GivenLoggerOverride()
        {
            using var smith = WorkflowForge.CreateSmith();
            var customLogger = NullLogger.Instance;

            using var foundry = smith.CreateFoundry(customLogger);

            Assert.NotNull(foundry);
            Assert.NotEqual(Guid.Empty, foundry.ExecutionId);
        }

        [Fact]
        public void AssociateWorkflow_GivenCreateFoundryForWithWorkflow()
        {
            using var smith = WorkflowForge.CreateSmith();
            var workflow = WorkflowForge.CreateWorkflow($"Assoc-{_uniqueTestId}")
                .AddOperation("Op1", (_, ct) => Task.CompletedTask)
                .Build();

            using var foundry = smith.CreateFoundryFor(workflow);

            Assert.NotNull(foundry);
            Assert.NotEqual(Guid.Empty, foundry.ExecutionId);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullWorkflowForCreateFoundryFor()
        {
            using var smith = WorkflowForge.CreateSmith();

            Assert.Throws<ArgumentNullException>(() => smith.CreateFoundryFor(null!));
        }

        [Fact]
        public void ReturnFoundryWithSharedData_GivenCreateFoundryWithData()
        {
            using var smith = WorkflowForge.CreateSmith();
            var data = new ConcurrentDictionary<string, object?>();
            data["shared"] = 42;

            using var foundry = smith.CreateFoundryWithData(data);

            Assert.True(foundry.Properties.ContainsKey("shared"));
            Assert.Equal(42, foundry.Properties["shared"]);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullDataForCreateFoundryWithData()
        {
            using var smith = WorkflowForge.CreateSmith();

            Assert.Throws<ArgumentNullException>(() =>
                smith.CreateFoundryWithData(null!));
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullMiddleware()
        {
            using var smith = WorkflowForge.CreateSmith();

            Assert.Throws<ArgumentNullException>(() => smith.AddWorkflowMiddleware(null!));
        }

        [Fact]
        public void ThrowObjectDisposedException_GivenAddMiddlewareAfterDispose()
        {
            var smith = WorkflowForge.CreateSmith();
            smith.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
                smith.AddWorkflowMiddleware(new Mock<IWorkflowMiddleware>().Object));
        }

        [Fact]
        public void ThrowObjectDisposedException_GivenCreateFoundryAfterDispose()
        {
            var smith = WorkflowForge.CreateSmith();
            smith.Dispose();

            Assert.Throws<ObjectDisposedException>(() => smith.CreateFoundry());
        }

        [Fact]
        public void ThrowObjectDisposedException_GivenCreateFoundryForAfterDispose()
        {
            var smith = WorkflowForge.CreateSmith();
            smith.Dispose();
            var workflow = WorkflowForge.CreateWorkflow($"Disposed-{_uniqueTestId}")
                .AddOperation("Op1", (_, ct) => Task.CompletedTask)
                .Build();

            Assert.Throws<ObjectDisposedException>(() => smith.CreateFoundryFor(workflow));
        }

        [Fact]
        public void ThrowObjectDisposedException_GivenCreateFoundryWithDataAfterDispose()
        {
            var smith = WorkflowForge.CreateSmith();
            smith.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
                smith.CreateFoundryWithData(new ConcurrentDictionary<string, object?>()));
        }

        [Fact]
        public async Task ExecuteMiddleware_GivenWorkflowMiddlewareAdded()
        {
            using var smith = WorkflowForge.CreateSmith();
            var middlewareExecuted = false;

            var middleware = new Mock<IWorkflowMiddleware>();
            middleware
                .Setup(m => m.ExecuteAsync(
                    It.IsAny<IWorkflow>(),
                    It.IsAny<IWorkflowFoundry>(),
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<IWorkflow, IWorkflowFoundry, Func<Task>, CancellationToken>(
                    async (_, _, next, _) =>
                    {
                        middlewareExecuted = true;
                        await next();
                    });

            smith.AddWorkflowMiddleware(middleware.Object);

            var workflow = WorkflowForge.CreateWorkflow($"Middleware-{_uniqueTestId}")
                .AddOperation("Op1", (_, ct) => Task.CompletedTask)
                .Build();

            await smith.ForgeAsync(workflow);

            Assert.True(middlewareExecuted);
        }

        [Fact]
        public async Task EmitStartedAndCompletedEvents_GivenSuccessfulForgeAsync()
        {
            using var smith = WorkflowForge.CreateSmith();
            var startedFired = false;
            var completedFired = false;
            TimeSpan? reportedDuration = null;

            smith.WorkflowStarted += (_, args) => startedFired = true;
            smith.WorkflowCompleted += (_, args) =>
            {
                completedFired = true;
                reportedDuration = args.Duration;
            };

            var workflow = WorkflowForge.CreateWorkflow($"Events-{_uniqueTestId}")
                .AddOperation("Op1", (_, ct) => Task.CompletedTask)
                .Build();

            await smith.ForgeAsync(workflow);

            Assert.True(startedFired);
            Assert.True(completedFired);
            Assert.NotNull(reportedDuration);
        }

        [Fact]
        public async Task ThrowOperationCancelledAndNotFireCompleted_GivenCancellation()
        {
            using var smith = WorkflowForge.CreateSmith();
            var completedFired = false;

            smith.WorkflowCompleted += (_, _) => completedFired = true;

            using var cts = new CancellationTokenSource();
            var workflow = WorkflowForge.CreateWorkflow($"Cancel-{_uniqueTestId}")
                .AddOperation("SlowOp", async (_, ct) =>
                {
                    cts.Cancel();
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(1000, ct);
                })
                .Build();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                smith.ForgeAsync(workflow, cts.Token));

            Assert.False(completedFired);
        }

        [Fact]
        public async Task DisposeExcessFoundries_GivenPoolOverflow()
        {
            using var smith = WorkflowForge.CreateSmith();
            var poolSize = Environment.ProcessorCount * 2;

            var tasks = Enumerable.Range(0, poolSize + 10).Select(i =>
            {
                var wf = WorkflowForge.CreateWorkflow($"Overflow-{_uniqueTestId}-{i}")
                    .AddOperation($"Op-{i}", (_, ct) => Task.CompletedTask)
                    .Build();
                return smith.ForgeAsync(wf);
            }).ToArray();

            var ex = await Record.ExceptionAsync(() => Task.WhenAll(tasks));

            Assert.Null(ex);
        }

        [Fact]
        public async Task ThrottleCorrectly_GivenConcurrencyLimiterWithExplicitFoundry()
        {
            var options = new WorkflowForgeOptions { MaxConcurrentWorkflows = 1 };
            using var smith = WorkflowForge.CreateSmith(options: options);

            var executionOrder = new ConcurrentBag<int>();
            var tasks = Enumerable.Range(0, 3).Select(async i =>
            {
                using var foundry = smith.CreateFoundry();
                var workflow = WorkflowForge.CreateWorkflow($"ThrottleFoundry-{_uniqueTestId}-{i}")
                    .AddOperation($"Op-{i}", async (_, ct) =>
                    {
                        executionOrder.Add(i);
                        await Task.Delay(20, ct);
                    })
                    .Build();
                await smith.ForgeAsync(workflow, foundry);
            }).ToArray();

            await Task.WhenAll(tasks);

            Assert.Equal(3, executionOrder.Count);
        }

        [Fact]
        public async Task FireCompensationEvents_GivenOperationFailure()
        {
            using var smith = WorkflowForge.CreateSmith();
            var compensationTriggered = false;
            var compensationCompleted = false;

            smith.CompensationTriggered += (_, args) =>
            {
                compensationTriggered = true;
                Assert.NotNull(args.FailedOperationName);
            };
            smith.CompensationCompleted += (_, args) =>
            {
                compensationCompleted = true;
                Assert.True(args.SuccessCount >= 0);
            };

            var workflow = WorkflowForge.CreateWorkflow($"CompEvents-{_uniqueTestId}")
                .AddOperation("Op1", (_, ct) => Task.FromResult<object?>("done"))
                .AddOperation("FailOp", (_, ct) => throw new InvalidOperationException("fail"))
                .Build();

            await Assert.ThrowsAnyAsync<Exception>(() => smith.ForgeAsync(workflow));

            Assert.True(compensationTriggered);
            Assert.True(compensationCompleted);
        }

        [Fact]
        public void ThrowWorkflowForgeException_GivenInvalidOptions()
        {
            var options = new WorkflowForgeOptions { MaxConcurrentWorkflows = -1 };

            Assert.Throws<global::WorkflowForge.Exceptions.WorkflowForgeException>(() =>
                WorkflowForge.CreateSmith(options: options));
        }

        [Fact]
        public async Task ThrowArgumentNullException_GivenNullWorkflow()
        {
            using var smith = WorkflowForge.CreateSmith();

            await Assert.ThrowsAsync<ArgumentNullException>(() => smith.ForgeAsync(null!));
        }

        [Fact]
        public async Task FireOperationRestoreEvents_GivenCompensationDuringFailure()
        {
            using var smith = WorkflowForge.CreateSmith();
            var restoreStarted = false;
            var restoreCompleted = false;

            smith.OperationRestoreStarted += (_, args) =>
            {
                restoreStarted = true;
                Assert.NotNull(args.Operation);
            };
            smith.OperationRestoreCompleted += (_, args) =>
            {
                restoreCompleted = true;
                Assert.True(args.Duration >= TimeSpan.Zero);
            };

            var workflow = WorkflowForge.CreateWorkflow($"RestoreEvents-{_uniqueTestId}")
                .AddOperation("Op1", (_, ct) => Task.FromResult<object?>("data"))
                .AddOperation("FailOp", (_, ct) => throw new InvalidOperationException("oops"))
                .Build();

            await Assert.ThrowsAnyAsync<Exception>(() => smith.ForgeAsync(workflow));

            Assert.True(restoreStarted);
            Assert.True(restoreCompleted);
        }
    }
}
