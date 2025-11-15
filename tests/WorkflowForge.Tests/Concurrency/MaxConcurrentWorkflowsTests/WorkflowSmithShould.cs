using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Loggers;
using WorkflowForge.Operations;
using WorkflowForge.Options;

namespace WorkflowForge.Tests.Concurrency.MaxConcurrentWorkflowsTests
{
    /// <summary>
    /// Tests for MaxConcurrentWorkflows throttling in WorkflowSmith
    /// </summary>
    public class WorkflowSmithShould : IDisposable
    {
        private readonly string _uniqueTestId;
        private readonly string _testDirectory;

        public WorkflowSmithShould()
        {
            _uniqueTestId = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}_{Thread.CurrentThread.ManagedThreadId}";
            _testDirectory = Path.Combine(Path.GetTempPath(), "WorkflowForgeTests", GetType().Name, _uniqueTestId);
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            SafeDeleteDirectory(_testDirectory);
        }

        private static void SafeDeleteDirectory(string path)
        {
            if (!Directory.Exists(path)) return;

            const int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    Directory.Delete(path, true);
                    return;
                }
                catch (IOException) when (attempt < maxRetries - 1)
                {
                    Thread.Sleep(100 * (attempt + 1));
                }
                catch
                {
                    break;
                }
            }
        }

        [Fact]
        public async Task AllowUnlimitedConcurrency_GivenZeroMaxConcurrentWorkflows()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var options = new WorkflowForgeOptions
            {
                MaxConcurrentWorkflows = 0 // Unlimited
            };
            var smith = new WorkflowSmith(logger, null, options);

            // Create simple workflows
            var workflows = Enumerable.Range(0, 100)
                .Select(i => WorkflowForge.CreateWorkflow()
                    .WithName($"workflow-{i}")
                    .AddOperation(new DelegateWorkflowOperation<string, string>(
                        $"op-{i}",
                        (input, foundry, ct) => Task.FromResult($"result-{i}")))
                    .Build())
                .ToList();

            // Act
            var tasks = workflows.Select(wf => smith.ForgeAsync(wf));
            await Task.WhenAll(tasks);

            // Assert - All workflows completed without throttling
            Assert.True(true); // If we got here, no deadlock occurred
        }

        [Fact]
        public async Task ThrottleConcurrency_GivenMaxConcurrentWorkflows()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var options = new WorkflowForgeOptions
            {
                MaxConcurrentWorkflows = 5
            };
            var smith = new WorkflowSmith(logger, null, options);

            var concurrencyTracker = new ConcurrencyTracker();
            var maxObservedConcurrency = 0;
            var lockObj = new object();

            // Create workflows that track concurrency
            var workflows = Enumerable.Range(0, 20)
                .Select(i => WorkflowForge.CreateWorkflow()
                    .WithName($"workflow-{i}")
                    .AddOperation(new DelegateWorkflowOperation<string, string>(
                        $"op-{i}",
                        async (input, foundry, ct) =>
                        {
                            var currentConcurrency = concurrencyTracker.Increment();
                            lock (lockObj)
                            {
                                if (currentConcurrency > maxObservedConcurrency)
                                {
                                    maxObservedConcurrency = currentConcurrency;
                                }
                            }

                            await Task.Delay(100, ct); // Simulate work
                            concurrencyTracker.Decrement();
                            return $"result-{i}";
                        }))
                    .Build())
                .ToList();

            // Act
            var tasks = workflows.Select(wf => smith.ForgeAsync(wf));
            await Task.WhenAll(tasks);

            // Assert
            Assert.True(maxObservedConcurrency <= 5, $"Max concurrency was {maxObservedConcurrency}, expected <= 5");
        }

        [Fact]
        public void ThrowException_GivenInvalidMaxConcurrentWorkflows()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var options = new WorkflowForgeOptions
            {
                MaxConcurrentWorkflows = -1 // Invalid
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new WorkflowSmith(logger, null, options));
            Assert.Contains("Invalid WorkflowForge options", ex.Message);
            Assert.Contains("MaxConcurrentWorkflows", ex.Message);
        }

        [Fact]
        public async Task NotBlockWorkflows_GivenNoConfiguration()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var smith = new WorkflowSmith(logger); // No configuration

            // Create simple workflows
            var workflows = Enumerable.Range(0, 50)
                .Select(i => WorkflowForge.CreateWorkflow()
                    .WithName($"workflow-{i}")
                    .AddOperation(new DelegateWorkflowOperation<string, string>(
                        $"op-{i}",
                        (input, foundry, ct) => Task.FromResult($"result-{i}")))
                    .Build())
                .ToList();

            // Act
            var tasks = workflows.Select(wf => smith.ForgeAsync(wf));
            await Task.WhenAll(tasks);

            // Assert - All workflows completed without throttling
            Assert.True(true); // If we got here, no deadlock occurred
        }

        /// <summary>
        /// Helper class to track concurrent executions safely
        /// </summary>
        private class ConcurrencyTracker
        {
            private int _currentCount;

            public int Increment()
            {
                return Interlocked.Increment(ref _currentCount);
            }

            public void Decrement()
            {
                Interlocked.Decrement(ref _currentCount);
            }

            public int CurrentCount => _currentCount;
        }
    }
}