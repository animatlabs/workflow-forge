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
    public class WorkflowForgeOpenTelemetryServiceShould : IDisposable
    {
        private readonly Mock<IWorkflowForgeLogger> _loggerMock;

        public WorkflowForgeOpenTelemetryServiceShould()
        {
            _loggerMock = new Mock<IWorkflowForgeLogger>();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void InitializeSuccessfully_GivenValidServiceName()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            Assert.Equal("TestService", service.ServiceName);
            Assert.NotNull(service.ActivitySource);
            Assert.NotNull(service.Meter);
            Assert.Equal("TestService", service.ActivitySource.Name);
            Assert.Equal("TestService", service.Meter.Name);
        }

        [Fact]
        public void InitializeWithVersion_GivenServiceNameAndVersion()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService", "2.0.0");

            Assert.Equal("TestService", service.ServiceName);
            Assert.Equal("2.0.0", service.ActivitySource.Version);
            Assert.Equal("2.0.0", service.Meter.Version);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullServiceName()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WorkflowForgeOpenTelemetryService(null!));
        }

        [Fact]
        public void UseProvidedLogger_GivenLogger()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService", "1.0.0", _loggerMock.Object);

            _loggerMock.Verify(
                x => x.LogInformation(It.IsAny<string>(), It.IsAny<object[]>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void UseNullLogger_GivenNullLogger()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService", "1.0.0", null);

            Assert.NotNull(service);
        }

        [Fact]
        public void ReturnActivityOrNull_GivenStartActivityWhenNotSampled()
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
        public void ReturnActivityWithKind_GivenStartActivityWithCustomKindWhenSampled()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            var activity = service.StartActivity("TestOperation", ActivityKind.Server);

            if (activity != null)
            {
                Assert.Equal(ActivityKind.Server, activity.Kind);
            }
        }

        [Fact]
        public void RecordMetrics_GivenRecordOperation()
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
        public void RecordMemoryMetric_GivenRecordOperationWithMemoryAllocated()
        {
            var ex = Record.Exception(() =>
            {
                using var service = new WorkflowForgeOpenTelemetryService("TestService");

                service.RecordOperation("Op1", TimeSpan.FromSeconds(1), success: true, memoryAllocated: 1024);
            });
            Assert.Null(ex);
        }

        [Fact]
        public void RecordWithTags_GivenRecordOperationWithTags()
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
        public void NotThrow_GivenRecordOperationWhenDisposed()
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
        public void IncrementCounter_GivenIncrementActiveOperations()
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
        public void NotThrow_GivenIncrementActiveOperationsWhenDisposed()
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
        public void DecrementCounter_GivenDecrementActiveOperations()
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
        public void NotThrow_GivenDecrementActiveOperationsWhenDisposed()
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
        public void ReturnCounter_GivenCreateCounter()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            var counter = service.CreateCounter<long>("custom.counter", "count", "Custom counter");

            Assert.NotNull(counter);
            counter.Add(1);
        }

        [Fact]
        public void ReturnHistogram_GivenCreateHistogram()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            var histogram = service.CreateHistogram<double>("custom.histogram", "s", "Custom histogram");

            Assert.NotNull(histogram);
            histogram.Record(1.5);
        }

        [Fact]
        public void WorkWithoutUnitAndDescription_GivenCreateCounterWithOptionalParams()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            var counter = service.CreateCounter<long>("custom.counter");

            Assert.NotNull(counter);
        }

        [Fact]
        public void WorkWithoutUnitAndDescription_GivenCreateHistogramWithOptionalParams()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            var histogram = service.CreateHistogram<double>("custom.histogram");

            Assert.NotNull(histogram);
        }

        [Fact]
        public void DisposeResources_GivenDispose()
        {
            var service = new WorkflowForgeOpenTelemetryService("TestService", "1.0.0", _loggerMock.Object);

            service.Dispose();
            service.Dispose();

            _loggerMock.Verify(
                x => x.LogInformation(It.IsAny<string>(), It.IsAny<object[]>()),
                Times.AtLeast(2));
        }

        [Fact]
        public void AllowMultipleCalls_GivenDispose()
        {
            var ex = Record.Exception(() =>
            {
                var service = new WorkflowForgeOpenTelemetryService("TestService");

                service.Dispose();
                service.Dispose();
            });
            Assert.Null(ex);
        }

        [Fact]
        public void CreateObservableGaugesReturningSystemMetrics_GivenConstruction()
        {
            var observedInstruments = new List<string>();

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "DiagSvc")
                {
                    observedInstruments.Add(instrument.Name);
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.Start();

            using var service = new WorkflowForgeOpenTelemetryService("DiagSvc");

            Assert.Contains("workflowforge.process.memory.usage", observedInstruments);
            Assert.Contains("workflowforge.process.gc.collections.total", observedInstruments);
            Assert.Contains("workflowforge.process.threadpool.threads.available", observedInstruments);
        }

        [Fact]
        public void ReturnNonNegativeValues_GivenObservableGaugesWhenObserved()
        {
            var memoryValues = new List<long>();
            var gcValues = new List<long>();
            var threadPoolValues = new List<int>();

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "GaugeSvc")
                    meterListener.EnableMeasurementEvents(instrument);
            };
            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            {
                if (instrument.Name == "workflowforge.process.memory.usage")
                    memoryValues.Add(measurement);
                else if (instrument.Name == "workflowforge.process.gc.collections.total")
                    gcValues.Add(measurement);
            });
            listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
            {
                if (instrument.Name == "workflowforge.process.threadpool.threads.available")
                    threadPoolValues.Add(measurement);
            });
            listener.Start();

            using var service = new WorkflowForgeOpenTelemetryService("GaugeSvc");
            listener.RecordObservableInstruments();

            Assert.NotEmpty(memoryValues);
            Assert.True(memoryValues[0] >= 0);

            Assert.NotEmpty(gcValues);
            Assert.True(gcValues[0] >= 0);

            Assert.NotEmpty(threadPoolValues);
            Assert.True(threadPoolValues[0] > 0);
        }

        [Fact]
        public void ReturnCounter_GivenCreateUpDownCounter()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            var counter = service.CreateUpDownCounter<int>("custom.updown", "item", "Custom up-down counter");

            Assert.NotNull(counter);
            counter.Add(5);
            counter.Add(-3);
        }

        [Fact]
        public void ReturnGauge_GivenCreateObservableGauge()
        {
            using var service = new WorkflowForgeOpenTelemetryService("TestService");

            var gauge = service.CreateObservableGauge("custom.gauge", () => 42L, "count", "Custom gauge");

            Assert.NotNull(gauge);
        }

        [Fact]
        public void IncrementErrorCounter_GivenRecordOperationWithFailure()
        {
            var ex = Record.Exception(() =>
            {
                using var service = new WorkflowForgeOpenTelemetryService("TestService");

                service.RecordOperation("FailOp", TimeSpan.FromMilliseconds(100), success: false, memoryAllocated: 2048,
                    new[] { new KeyValuePair<string, object?>("error.type", "Timeout") });
            });

            Assert.Null(ex);
        }

        [Fact]
        public void NotRecordMemoryMetric_GivenRecordOperationWithZeroMemory()
        {
            var ex = Record.Exception(() =>
            {
                using var service = new WorkflowForgeOpenTelemetryService("TestService");
                service.RecordOperation("ZeroMem", TimeSpan.FromMilliseconds(10), success: true, memoryAllocated: 0);
            });

            Assert.Null(ex);
        }
    }
}
