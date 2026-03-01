using System;
using System.Collections.Concurrent;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Observability.Performance;
using WorkflowForge.Extensions.Observability.Performance.Abstractions;
using WorkflowForge.Testing;
using Moq;
using Xunit;

namespace WorkflowForge.Tests.Extensions.Performance
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

        [Fact]
        public void ThrowArgumentNullException_GivenNullFoundryForGetPerformanceStatistics()
        {
            IWorkflowFoundry? foundry = null;
            Assert.Throws<ArgumentNullException>(() => foundry!.GetPerformanceStatistics());
        }

        [Fact]
        public void ReturnNull_GivenPerformanceStatisticsNotSet()
        {
            var stats = _foundry.GetPerformanceStatistics();
            Assert.Null(stats);
        }

        [Fact]
        public void ReturnStatistics_GivenPerformanceStatisticsStoredInProperties()
        {
            var statsMock = new Mock<IFoundryPerformanceStatistics>();
            statsMock.Setup(s => s.TotalOperations).Returns(5);
            _foundry.Properties["PerformanceStatistics"] = statsMock.Object;

            var stats = _foundry.GetPerformanceStatistics();

            Assert.NotNull(stats);
            Assert.Equal(5, stats!.TotalOperations);
        }

        [Fact]
        public void ReturnNull_GivenPropertyIsNotPerformanceStatisticsType()
        {
            _foundry.Properties["PerformanceStatistics"] = "not a stats object";

            var stats = _foundry.GetPerformanceStatistics();

            Assert.Null(stats);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullFoundryForEnablePerformanceMonitoring()
        {
            IWorkflowFoundry? foundry = null;
            Assert.Throws<ArgumentNullException>(() => foundry!.EnablePerformanceMonitoring());
        }

        [Fact]
        public void ReturnFalse_GivenFoundryDoesNotImplementIPerformanceMonitoredFoundry()
        {
            var result = _foundry.EnablePerformanceMonitoring();
            Assert.False(result);
        }

        [Fact]
        public void ReturnTrue_GivenFoundryImplementsIPerformanceMonitoredFoundry()
        {
            var mockFoundry = new Mock<IWorkflowFoundry>();
            var mockPerfFoundry = mockFoundry.As<IPerformanceMonitoredFoundry>();
            mockPerfFoundry.Setup(f => f.EnablePerformanceMonitoring()).Returns(true);

            var result = mockFoundry.Object.EnablePerformanceMonitoring();

            Assert.True(result);
            mockPerfFoundry.Verify(f => f.EnablePerformanceMonitoring(), Times.Once);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullFoundryForDisablePerformanceMonitoring()
        {
            IWorkflowFoundry? foundry = null;
            Assert.Throws<ArgumentNullException>(() => foundry!.DisablePerformanceMonitoring());
        }

        [Fact]
        public void ReturnFalse_GivenFoundryDoesNotImplementIPerformanceMonitoredFoundryForDisable()
        {
            var result = _foundry.DisablePerformanceMonitoring();
            Assert.False(result);
        }

        [Fact]
        public void ReturnTrue_GivenFoundryImplementsIPerformanceMonitoredFoundryForDisable()
        {
            var mockFoundry = new Mock<IWorkflowFoundry>();
            var mockPerfFoundry = mockFoundry.As<IPerformanceMonitoredFoundry>();
            mockPerfFoundry.Setup(f => f.DisablePerformanceMonitoring()).Returns(true);

            var result = mockFoundry.Object.DisablePerformanceMonitoring();

            Assert.True(result);
            mockPerfFoundry.Verify(f => f.DisablePerformanceMonitoring(), Times.Once);
        }
    }
}
