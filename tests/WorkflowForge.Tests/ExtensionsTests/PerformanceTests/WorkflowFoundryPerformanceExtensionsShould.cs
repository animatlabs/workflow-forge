using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Observability.Performance;
using WorkflowForge.Extensions.Observability.Performance.Abstractions;
using WorkflowForge.Operations;
using WorkflowForge.Testing;

namespace WorkflowForge.Tests.ExtensionsTests.PerformanceTests
{
    public class WorkflowFoundryPerformanceExtensionsShould : IDisposable
    {
        private readonly FakeWorkflowFoundry _foundry;

        public WorkflowFoundryPerformanceExtensionsShould()
        {
            _foundry = new FakeWorkflowFoundry();
        }

        public void Dispose()
        {
            _foundry.Dispose();
        }

        // --------------------------------------------------------------------------------------
        // Null-argument guards
        // --------------------------------------------------------------------------------------

        [Fact]
        public void ThrowArgumentNullException_GivenNullFoundryForGetPerformanceStatistics()
        {
            IWorkflowFoundry? foundry = null;
            Assert.Throws<ArgumentNullException>(() => foundry!.GetPerformanceStatistics());
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullFoundryForEnablePerformanceMonitoring()
        {
            IWorkflowFoundry? foundry = null;
            Assert.Throws<ArgumentNullException>(() => foundry!.EnablePerformanceMonitoring());
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullFoundryForDisablePerformanceMonitoring()
        {
            IWorkflowFoundry? foundry = null;
            Assert.Throws<ArgumentNullException>(() => foundry!.DisablePerformanceMonitoring());
        }

        // --------------------------------------------------------------------------------------
        // GetPerformanceStatistics basics
        // --------------------------------------------------------------------------------------

        [Fact]
        public void ReturnNull_GivenPerformanceStatisticsNotSet()
        {
            var stats = _foundry.GetPerformanceStatistics();
            Assert.Null(stats);
        }

        [Fact]
        public void ReturnNull_GivenPropertyIsNotPerformanceStatisticsType()
        {
            _foundry.Properties["PerformanceStatistics"] = "not a stats object";

            var stats = _foundry.GetPerformanceStatistics();

            Assert.Null(stats);
        }

        // --------------------------------------------------------------------------------------
        // Standard foundry: EnablePerformanceMonitoring now wires up real monitoring
        // --------------------------------------------------------------------------------------

        [Fact]
        public void ReturnTrueAndStoreStatistics_WhenEnabledOnStandardFoundry()
        {
            var result = _foundry.EnablePerformanceMonitoring();

            Assert.True(result);
            Assert.NotNull(_foundry.GetPerformanceStatistics());
        }

        [Fact]
        public void RemoveStatistics_WhenDisabledOnStandardFoundry()
        {
            _foundry.EnablePerformanceMonitoring();

            var disabled = _foundry.DisablePerformanceMonitoring();

            Assert.True(disabled);
            Assert.Null(_foundry.GetPerformanceStatistics());
        }

        [Fact]
        public void ReturnFalse_WhenDisablingMonitoringThatWasNeverEnabled()
        {
            var disabled = _foundry.DisablePerformanceMonitoring();
            Assert.False(disabled);
        }

        // --------------------------------------------------------------------------------------
        // Native IPerformanceMonitoredFoundry fast-path
        // --------------------------------------------------------------------------------------

        [Fact]
        public void DelegateToNativeFoundry_ForEnable()
        {
            var mockFoundry = new Mock<IWorkflowFoundry>();
            var mockPerfFoundry = mockFoundry.As<IPerformanceMonitoredFoundry>();
            mockPerfFoundry.Setup(f => f.EnablePerformanceMonitoring()).Returns(true);

            var result = mockFoundry.Object.EnablePerformanceMonitoring();

            Assert.True(result);
            mockPerfFoundry.Verify(f => f.EnablePerformanceMonitoring(), Times.Once);
        }

        [Fact]
        public void DelegateToNativeFoundry_ForDisable()
        {
            var mockFoundry = new Mock<IWorkflowFoundry>();
            var mockPerfFoundry = mockFoundry.As<IPerformanceMonitoredFoundry>();
            mockPerfFoundry.Setup(f => f.DisablePerformanceMonitoring()).Returns(true);

            var result = mockFoundry.Object.DisablePerformanceMonitoring();

            Assert.True(result);
            mockPerfFoundry.Verify(f => f.DisablePerformanceMonitoring(), Times.Once);
        }

        [Fact]
        public void DelegateToNativeFoundry_ForGetStatistics()
        {
            var stats = new Mock<IFoundryPerformanceStatistics>().Object;
            var mockFoundry = new Mock<IWorkflowFoundry>();
            var mockPerfFoundry = mockFoundry.As<IPerformanceMonitoredFoundry>();
            mockPerfFoundry.Setup(f => f.GetPerformanceStatistics()).Returns(stats);

            var result = mockFoundry.Object.GetPerformanceStatistics();

            Assert.Same(stats, result);
            mockPerfFoundry.Verify(f => f.GetPerformanceStatistics(), Times.Once);
        }

        // --------------------------------------------------------------------------------------
        // End-to-end through the real foundry + smith (the documented usage). This is the path the
        // previous mock-injection tests never exercised, which is why the feature shipped broken.
        // --------------------------------------------------------------------------------------

        [Fact]
        public async Task RecordStatistics_WhenWorkflowRunsThroughSmith()
        {
            using var foundry = WorkflowForge.CreateFoundry("PerfTest");
            Assert.True(foundry.EnablePerformanceMonitoring());

            using var smith = WorkflowForge.CreateSmith();
            var workflow = WorkflowForge.CreateWorkflow("PerfTest")
                .AddOperation(new ActionWorkflowOperation("A", (input, f, ct) => Task.CompletedTask))
                .AddOperation(new ActionWorkflowOperation("B", (input, f, ct) => Task.CompletedTask))
                .Build();

            await smith.ForgeAsync(workflow, foundry);

            var stats = foundry.GetPerformanceStatistics();
            Assert.NotNull(stats);
            Assert.Equal(2, stats!.TotalOperations);
            Assert.Equal(2, stats.SuccessfulOperations);
            Assert.Equal(0, stats.FailedOperations);
            Assert.Equal(1.0, stats.SuccessRate);
            Assert.Equal(2, stats.GetAllOperationStatistics().Count);
            Assert.NotNull(stats.GetOperationStatistics("A"));
        }

        [Fact]
        public async Task RecordFailure_WhenOperationThrows()
        {
            using var foundry = WorkflowForge.CreateFoundry("PerfFailTest");
            foundry.EnablePerformanceMonitoring();

            using var smith = WorkflowForge.CreateSmith();
            var workflow = WorkflowForge.CreateWorkflow("PerfFailTest")
                .AddOperation(new ActionWorkflowOperation("Ok", (input, f, ct) => Task.CompletedTask))
                .AddOperation(new ActionWorkflowOperation("Boom", (input, f, ct) => throw new InvalidOperationException("boom")))
                .Build();

            await Assert.ThrowsAnyAsync<Exception>(() => smith.ForgeAsync(workflow, foundry));

            var stats = foundry.GetPerformanceStatistics();
            Assert.NotNull(stats);
            Assert.Equal(2, stats!.TotalOperations);
            Assert.Equal(1, stats.SuccessfulOperations);
            Assert.Equal(1, stats.FailedOperations);
        }

        [Fact]
        public async Task NotDoubleCount_WhenMonitoringEnabledTwice()
        {
            using var foundry = WorkflowForge.CreateFoundry("PerfIdempotentTest");
            foundry.EnablePerformanceMonitoring();
            foundry.EnablePerformanceMonitoring(); // must not register a second middleware

            using var smith = WorkflowForge.CreateSmith();
            var workflow = WorkflowForge.CreateWorkflow("PerfIdempotentTest")
                .AddOperation(new ActionWorkflowOperation("Only", (input, f, ct) => Task.CompletedTask))
                .Build();

            await smith.ForgeAsync(workflow, foundry);

            var stats = foundry.GetPerformanceStatistics();
            Assert.NotNull(stats);
            Assert.Equal(1, stats!.TotalOperations);
        }
    }
}
