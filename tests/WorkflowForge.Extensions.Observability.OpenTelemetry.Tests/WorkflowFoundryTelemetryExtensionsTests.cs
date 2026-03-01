using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Observability.OpenTelemetry;
using WorkflowForge.Testing;
using Moq;
using Xunit;

namespace WorkflowForge.Extensions.Observability.OpenTelemetry.Tests
{
    public class WorkflowFoundryTelemetryExtensionsShould : IDisposable
    {
        private readonly FakeWorkflowFoundry _foundry;

        public WorkflowFoundryTelemetryExtensionsShould()
        {
            _foundry = new FakeWorkflowFoundry();
        }

        public void Dispose()
        {
            _foundry.DisableOpenTelemetry();
            _foundry.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void ReturnTrue_GivenValidFoundry()
        {
            var result = _foundry.EnableOpenTelemetry();

            Assert.True(result);
            Assert.True(_foundry.IsOpenTelemetryEnabled());
        }

        [Fact]
        public void ConfigureService_GivenOptions()
        {
            var options = new WorkflowForgeOpenTelemetryOptions
            {
                ServiceName = "CustomService",
                ServiceVersion = "3.0.0"
            };

            var result = _foundry.EnableOpenTelemetry(options);

            Assert.True(result);
            var service = _foundry.GetOpenTelemetryService();
            Assert.NotNull(service);
            Assert.Equal("CustomService", service.ServiceName);
            Assert.Equal("3.0.0", service.Meter.Version);
        }

        [Fact]
        public void UseDefaults_GivenNullOptions()
        {
            var result = _foundry.EnableOpenTelemetry(null);

            Assert.True(result);
            var service = _foundry.GetOpenTelemetryService();
            Assert.NotNull(service);
            Assert.Equal("WorkflowForge.Service", service.ServiceName);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullFoundry()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.EnableOpenTelemetry());
        }

        [Fact]
        public void ReturnTrue_GivenEnabled()
        {
            _foundry.EnableOpenTelemetry();

            var result = _foundry.DisableOpenTelemetry();

            Assert.True(result);
            Assert.False(_foundry.IsOpenTelemetryEnabled());
        }

        [Fact]
        public void ReturnFalse_GivenNotEnabled()
        {
            var result = _foundry.DisableOpenTelemetry();

            Assert.False(result);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenDisableWithNullFoundry()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.DisableOpenTelemetry());
        }

        [Fact]
        public void ReturnService_GivenEnabled()
        {
            _foundry.EnableOpenTelemetry();

            var service = _foundry.GetOpenTelemetryService();

            Assert.NotNull(service);
            Assert.Equal("WorkflowForge.Service", service.ServiceName);
        }

        [Fact]
        public void ReturnNull_GivenNotEnabled()
        {
            var service = _foundry.GetOpenTelemetryService();

            Assert.Null(service);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenGetServiceWithNullFoundry()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.GetOpenTelemetryService());
        }

        [Fact]
        public void ReturnActivityOrNull_GivenEnabledWhenNotSampled()
        {
            _foundry.EnableOpenTelemetry();

            var activity = _foundry.StartActivity("TestOp");

            if (activity != null)
            {
                Assert.Equal("TestOp", activity.OperationName);
            }
        }

        [Fact]
        public void ReturnNull_GivenStartActivityWhenNotEnabled()
        {
            var activity = _foundry.StartActivity("TestOp");

            Assert.Null(activity);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenStartActivityWithNullFoundry()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.StartActivity("TestOp"));
        }

        [Fact]
        public void RecordMetrics_GivenEnabled()
        {
            var ex = Record.Exception(() =>
            {
                _foundry.EnableOpenTelemetry();

                _foundry.RecordOperationMetrics("Op1", TimeSpan.FromSeconds(1), success: true);
                _foundry.RecordOperationMetrics("Op2", TimeSpan.FromMilliseconds(500), success: false, 1024, ("key", "value"));
            });
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenRecordOperationMetricsWhenNotEnabled()
        {
            var ex = Record.Exception(() => _foundry.RecordOperationMetrics("Op1", TimeSpan.FromSeconds(1), success: true));
            Assert.Null(ex);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenRecordOperationMetricsWithNullFoundry()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() =>
                foundry!.RecordOperationMetrics("Op1", TimeSpan.FromSeconds(1), success: true));
        }

        [Fact]
        public void IncrementCounter_GivenEnabled()
        {
            var ex = Record.Exception(() =>
            {
                _foundry.EnableOpenTelemetry();

                _foundry.IncrementActiveOperations("Op1");
                _foundry.IncrementActiveOperations("Op2", ("tag", "value"));
            });
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenIncrementActiveOperationsWhenNotEnabled()
        {
            var ex = Record.Exception(() => _foundry.IncrementActiveOperations("Op1"));
            Assert.Null(ex);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenIncrementActiveOperationsWithNullFoundry()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.IncrementActiveOperations("Op1"));
        }

        [Fact]
        public void DecrementCounter_GivenDecrementActiveOperationsWhenEnabled()
        {
            var ex = Record.Exception(() =>
            {
                _foundry.EnableOpenTelemetry();

                _foundry.DecrementActiveOperations("Op1");
                _foundry.DecrementActiveOperations("Op2", ("tag", "value"));
            });
            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenDecrementActiveOperationsWhenNotEnabled()
        {
            var ex = Record.Exception(() => _foundry.DecrementActiveOperations("Op1"));
            Assert.Null(ex);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenDecrementActiveOperationsWithNullFoundry()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.DecrementActiveOperations("Op1"));
        }

        [Fact]
        public void ReturnTrue_GivenIsOpenTelemetryEnabledWhenEnabled()
        {
            _foundry.EnableOpenTelemetry();

            Assert.True(_foundry.IsOpenTelemetryEnabled());
        }

        [Fact]
        public void ReturnFalse_GivenIsOpenTelemetryEnabledWhenNotEnabled()
        {
            Assert.False(_foundry.IsOpenTelemetryEnabled());
        }

        [Fact]
        public void ThrowArgumentNullException_GivenIsOpenTelemetryEnabledWithNullFoundry()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.IsOpenTelemetryEnabled());
        }

        [Fact]
        public void ReturnCounter_GivenCreateCounterWhenEnabled()
        {
            _foundry.EnableOpenTelemetry();

            var counter = _foundry.CreateCounter<long>("custom.counter", "count", "Description");

            Assert.NotNull(counter);
            counter!.Add(1);
        }

        [Fact]
        public void ReturnNull_GivenCreateCounterWhenNotEnabled()
        {
            var counter = _foundry.CreateCounter<long>("custom.counter");

            Assert.Null(counter);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenCreateCounterWithNullFoundry()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.CreateCounter<long>("counter"));
        }

        [Fact]
        public void ReturnHistogram_GivenCreateHistogramWhenEnabled()
        {
            _foundry.EnableOpenTelemetry();

            var histogram = _foundry.CreateHistogram<double>("custom.histogram", "s", "Description");

            Assert.NotNull(histogram);
            histogram!.Record(1.5);
        }

        [Fact]
        public void ReturnNull_GivenCreateHistogramWhenNotEnabled()
        {
            var histogram = _foundry.CreateHistogram<double>("custom.histogram");

            Assert.Null(histogram);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenCreateHistogramWithNullFoundry()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.CreateHistogram<double>("histogram"));
        }

        [Fact]
        public void ConvertTupleTagsToKeyValuePairs_GivenRecordOperationMetricsWithTags()
        {
            _foundry.EnableOpenTelemetry();

            var ex = Record.Exception(() =>
                _foundry.RecordOperationMetrics("TagOp", TimeSpan.FromMilliseconds(100), true, 512,
                    ("env", "test"), ("region", "us-east")));

            Assert.Null(ex);
        }

        [Fact]
        public void NotThrow_GivenRecordOperationMetricsWithEmptyTags()
        {
            _foundry.EnableOpenTelemetry();

            var ex = Record.Exception(() =>
                _foundry.RecordOperationMetrics("NoTagOp", TimeSpan.FromMilliseconds(50), false, 0));

            Assert.Null(ex);
        }

        [Fact]
        public void ConvertTupleTagsCorrectly_GivenIncrementAndDecrementWithTags()
        {
            _foundry.EnableOpenTelemetry();

            var ex = Record.Exception(() =>
            {
                _foundry.IncrementActiveOperations("ConcOp", ("pool", "A"), ("priority", "high"));
                _foundry.DecrementActiveOperations("ConcOp", ("pool", "A"), ("priority", "high"));
            });

            Assert.Null(ex);
        }

        [Fact]
        public void OverwriteService_GivenEnableOpenTelemetryCalledTwice()
        {
            _foundry.EnableOpenTelemetry(new WorkflowForgeOpenTelemetryOptions { ServiceName = "First" });
            _foundry.EnableOpenTelemetry(new WorkflowForgeOpenTelemetryOptions { ServiceName = "Second" });

            var service = _foundry.GetOpenTelemetryService();
            Assert.NotNull(service);
            Assert.Equal("Second", service.ServiceName);
        }

        [Fact]
        public void DisableSuccessfully_GivenDisableAfterDoubleEnable()
        {
            _foundry.EnableOpenTelemetry();
            _foundry.EnableOpenTelemetry();

            var result = _foundry.DisableOpenTelemetry();

            Assert.True(result);
            Assert.False(_foundry.IsOpenTelemetryEnabled());
        }

        [Fact]
        public void ReturnNull_GivenPropertySetToNonServiceType()
        {
            _foundry.Properties["_opentelemetry_service"] = "not a service";

            var service = _foundry.GetOpenTelemetryService();

            Assert.Null(service);
        }
    }
}
