using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Observability.OpenTelemetry;
using WorkflowForge.Testing;
using Moq;
using Xunit;

namespace WorkflowForge.Extensions.Observability.OpenTelemetry.Tests
{
    public class WorkflowForgeOpenTelemetryServiceTests : IDisposable
    {
        private readonly Mock<IWorkflowForgeLogger> _loggerMock;

        public WorkflowForgeOpenTelemetryServiceTests()
        {
            _loggerMock = new Mock<IWorkflowForgeLogger>();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void Constructor_WithValidServiceName_InitializesSuccessfully()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            Assert.Equal("TestService", service.ServiceName);
            Assert.NotNull(service.ActivitySource);
            Assert.NotNull(service.Meter);
            Assert.Equal("TestService", service.ActivitySource.Name);
            Assert.Equal("TestService", service.Meter.Name);
        }

        [Fact]
        public void Constructor_WithServiceNameAndVersion_InitializesWithVersion()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService", "2.0.0");

            Assert.Equal("TestService", service.ServiceName);
            Assert.Equal("2.0.0", service.ActivitySource.Version);
            Assert.Equal("2.0.0", service.Meter.Version);
        }

        [Fact]
        public void Constructor_WithNullServiceName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WorkflowForgeOpenTelemetryService(null!));
        }

        [Fact]
        public void Constructor_WithLogger_UsesProvidedLogger()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService", "1.0.0", _loggerMock.Object);

            _loggerMock.Verify(
                x => x.LogInformation(It.IsAny<string>(), It.IsAny<object[]>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void Constructor_WithNullLogger_UsesNullLogger()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService", "1.0.0", null);

            Assert.NotNull(service);
        }

        [Fact]
        public void StartActivity_ReturnsActivityOrNullWhenNotSampled()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            var activity = service.StartActivity("TestOperation");

            if (activity != null)
            {
                Assert.Equal("TestOperation", activity.OperationName);
                Assert.Equal(ActivityKind.Internal, activity.Kind);
            }
        }

        [Fact]
        public void StartActivity_WithCustomKind_ReturnsActivityWithKindWhenSampled()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            var activity = service.StartActivity("TestOperation", ActivityKind.Server);

            if (activity != null)
            {
                Assert.Equal(ActivityKind.Server, activity.Kind);
            }
        }

        [Fact]
        public void RecordOperation_RecordsMetrics()
        {
            var ex = Record.Exception(() =>
            {
                using var service = new WorkflowForgeOpenTelemetryService("TestService");

                service.RecordOperation("Op1", TimeSpan.FromSeconds(1), success: true);
                service.RecordOperation("Op2", TimeSpan.FromMilliseconds(500), success: false);
            });
            Assert.Null(ex);
        }

        [Fact]
        public void RecordOperation_WithMemoryAllocated_RecordsMemoryMetric()
        {
            var ex = Record.Exception(() =>
            {
                using var service = new WorkflowForgeOpenTelemetryService("TestService");

                service.RecordOperation("Op1", TimeSpan.FromSeconds(1), success: true, memoryAllocated: 1024);
            });
            Assert.Null(ex);
        }

        [Fact]
        public void RecordOperation_WithTags_RecordsWithTags()
        {
            var ex = Record.Exception(() =>
            {
                using var service = new WorkflowForgeOpenTelemetryService("TestService");
                var tags = new[] { new KeyValuePair<string, object?>("key", "value") };

                service.RecordOperation("Op1", TimeSpan.FromSeconds(1), success: true, 0, tags);
            });
            Assert.Null(ex);
        }

        [Fact]
        public void RecordOperation_WhenDisposed_DoesNotThrow()
        {
            var ex = Record.Exception(() =>
            {
                var service = new WorkflowForgeOpenTelemetryService("TestService");
                service.Dispose();

                service.RecordOperation("Op1", TimeSpan.FromSeconds(1), success: true);
            });
            Assert.Null(ex);
        }

        [Fact]
        public void IncrementActiveOperations_IncrementsCounter()
        {
            var ex = Record.Exception(() =>
            {
                using var service = new WorkflowForgeOpenTelemetryService("TestService");

                service.IncrementActiveOperations("Op1");
                service.IncrementActiveOperations("Op2", new[] { new KeyValuePair<string, object?>("tag", "value") });
            });
            Assert.Null(ex);
        }

        [Fact]
        public void IncrementActiveOperations_WhenDisposed_DoesNotThrow()
        {
            var ex = Record.Exception(() =>
            {
                var service = new WorkflowForgeOpenTelemetryService("TestService");
                service.Dispose();

                service.IncrementActiveOperations("Op1");
            });
            Assert.Null(ex);
        }

        [Fact]
        public void DecrementActiveOperations_DecrementsCounter()
        {
            var ex = Record.Exception(() =>
            {
                using var service = new WorkflowForgeOpenTelemetryService("TestService");

                service.DecrementActiveOperations("Op1");
                service.DecrementActiveOperations("Op2", new[] { new KeyValuePair<string, object?>("tag", "value") });
            });
            Assert.Null(ex);
        }

        [Fact]
        public void DecrementActiveOperations_WhenDisposed_DoesNotThrow()
        {
            var ex = Record.Exception(() =>
            {
                var service = new WorkflowForgeOpenTelemetryService("TestService");
                service.Dispose();

                service.DecrementActiveOperations("Op1");
            });
            Assert.Null(ex);
        }

        [Fact]
        public void CreateCounter_ReturnsCounter()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            var counter = service.CreateCounter<long>("custom.counter", "count", "Custom counter");

            Assert.NotNull(counter);
            counter.Add(1);
        }

        [Fact]
        public void CreateHistogram_ReturnsHistogram()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            var histogram = service.CreateHistogram<double>("custom.histogram", "s", "Custom histogram");

            Assert.NotNull(histogram);
            histogram.Record(1.5);
        }

        [Fact]
        public void CreateCounter_WithOptionalParams_WorksWithoutUnitAndDescription()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            var counter = service.CreateCounter<long>("custom.counter");

            Assert.NotNull(counter);
        }

        [Fact]
        public void CreateHistogram_WithOptionalParams_WorksWithoutUnitAndDescription()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            var histogram = service.CreateHistogram<double>("custom.histogram");

            Assert.NotNull(histogram);
        }

        [Fact]
        public void Dispose_DisposesResources()
        {
            var service = new WorkflowForgeOpenTelemetryService("TestService", "1.0.0", _loggerMock.Object);

            service.Dispose();
            service.Dispose();

            _loggerMock.Verify(
                x => x.LogInformation(It.IsAny<string>(), It.IsAny<object[]>()),
                Times.AtLeast(2));
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var ex = Record.Exception(() =>
            {
                var service = new WorkflowForgeOpenTelemetryService("TestService");

                service.Dispose();
                service.Dispose();
            });
            Assert.Null(ex);
        }
    }
}
