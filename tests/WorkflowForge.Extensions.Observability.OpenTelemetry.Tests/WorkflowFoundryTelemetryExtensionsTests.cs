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
    public class WorkflowFoundryTelemetryExtensionsTests : IDisposable
    {
        private readonly FakeWorkflowFoundry _foundry;

        public WorkflowFoundryTelemetryExtensionsTests()
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
        public void EnableOpenTelemetry_WithValidFoundry_ReturnsTrue()
        {
            var result = _foundry.EnableOpenTelemetry();

            Assert.True(result);
            Assert.True(_foundry.IsOpenTelemetryEnabled());
        }

        [Fact]
        public void EnableOpenTelemetry_WithOptions_ConfiguresService()
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
        public void EnableOpenTelemetry_WithNullOptions_UsesDefaults()
        {
            var result = _foundry.EnableOpenTelemetry(null);

            Assert.True(result);
            var service = _foundry.GetOpenTelemetryService();
            Assert.NotNull(service);
            Assert.Equal("WorkflowForge.Service", service.ServiceName);
        }

        [Fact]
        public void EnableOpenTelemetry_WithNullFoundry_ThrowsArgumentNullException()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.EnableOpenTelemetry());
        }

        [Fact]
        public void DisableOpenTelemetry_WhenEnabled_ReturnsTrue()
        {
            _foundry.EnableOpenTelemetry();

            var result = _foundry.DisableOpenTelemetry();

            Assert.True(result);
            Assert.False(_foundry.IsOpenTelemetryEnabled());
        }

        [Fact]
        public void DisableOpenTelemetry_WhenNotEnabled_ReturnsFalse()
        {
            var result = _foundry.DisableOpenTelemetry();

            Assert.False(result);
        }

        [Fact]
        public void DisableOpenTelemetry_WithNullFoundry_ThrowsArgumentNullException()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.DisableOpenTelemetry());
        }

        [Fact]
        public void GetOpenTelemetryService_WhenEnabled_ReturnsService()
        {
            _foundry.EnableOpenTelemetry();

            var service = _foundry.GetOpenTelemetryService();

            Assert.NotNull(service);
            Assert.Equal("WorkflowForge.Service", service.ServiceName);
        }

        [Fact]
        public void GetOpenTelemetryService_WhenNotEnabled_ReturnsNull()
        {
            var service = _foundry.GetOpenTelemetryService();

            Assert.Null(service);
        }

        [Fact]
        public void GetOpenTelemetryService_WithNullFoundry_ThrowsArgumentNullException()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.GetOpenTelemetryService());
        }

        [Fact]
        public void StartActivity_WhenEnabled_ReturnsActivityOrNullWhenNotSampled()
        {
            _foundry.EnableOpenTelemetry();

            var activity = _foundry.StartActivity("TestOp");

            if (activity != null)
            {
                Assert.Equal("TestOp", activity.OperationName);
            }
        }

        [Fact]
        public void StartActivity_WhenNotEnabled_ReturnsNull()
        {
            var activity = _foundry.StartActivity("TestOp");

            Assert.Null(activity);
        }

        [Fact]
        public void StartActivity_WithNullFoundry_ThrowsArgumentNullException()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.StartActivity("TestOp"));
        }

        [Fact]
        public void RecordOperationMetrics_WhenEnabled_RecordsMetrics()
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
        public void RecordOperationMetrics_WhenNotEnabled_DoesNotThrow()
        {
            var ex = Record.Exception(() => _foundry.RecordOperationMetrics("Op1", TimeSpan.FromSeconds(1), success: true));
            Assert.Null(ex);
        }

        [Fact]
        public void RecordOperationMetrics_WithNullFoundry_ThrowsArgumentNullException()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() =>
                foundry!.RecordOperationMetrics("Op1", TimeSpan.FromSeconds(1), success: true));
        }

        [Fact]
        public void IncrementActiveOperations_WhenEnabled_IncrementsCounter()
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
        public void IncrementActiveOperations_WhenNotEnabled_DoesNotThrow()
        {
            var ex = Record.Exception(() => _foundry.IncrementActiveOperations("Op1"));
            Assert.Null(ex);
        }

        [Fact]
        public void IncrementActiveOperations_WithNullFoundry_ThrowsArgumentNullException()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.IncrementActiveOperations("Op1"));
        }

        [Fact]
        public void DecrementActiveOperations_WhenEnabled_DecrementsCounter()
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
        public void DecrementActiveOperations_WhenNotEnabled_DoesNotThrow()
        {
            var ex = Record.Exception(() => _foundry.DecrementActiveOperations("Op1"));
            Assert.Null(ex);
        }

        [Fact]
        public void DecrementActiveOperations_WithNullFoundry_ThrowsArgumentNullException()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.DecrementActiveOperations("Op1"));
        }

        [Fact]
        public void IsOpenTelemetryEnabled_WhenEnabled_ReturnsTrue()
        {
            _foundry.EnableOpenTelemetry();

            Assert.True(_foundry.IsOpenTelemetryEnabled());
        }

        [Fact]
        public void IsOpenTelemetryEnabled_WhenNotEnabled_ReturnsFalse()
        {
            Assert.False(_foundry.IsOpenTelemetryEnabled());
        }

        [Fact]
        public void IsOpenTelemetryEnabled_WithNullFoundry_ThrowsArgumentNullException()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.IsOpenTelemetryEnabled());
        }

        [Fact]
        public void CreateCounter_WhenEnabled_ReturnsCounter()
        {
            _foundry.EnableOpenTelemetry();

            var counter = _foundry.CreateCounter<long>("custom.counter", "count", "Description");

            Assert.NotNull(counter);
            counter!.Add(1);
        }

        [Fact]
        public void CreateCounter_WhenNotEnabled_ReturnsNull()
        {
            var counter = _foundry.CreateCounter<long>("custom.counter");

            Assert.Null(counter);
        }

        [Fact]
        public void CreateCounter_WithNullFoundry_ThrowsArgumentNullException()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.CreateCounter<long>("counter"));
        }

        [Fact]
        public void CreateHistogram_WhenEnabled_ReturnsHistogram()
        {
            _foundry.EnableOpenTelemetry();

            var histogram = _foundry.CreateHistogram<double>("custom.histogram", "s", "Description");

            Assert.NotNull(histogram);
            histogram!.Record(1.5);
        }

        [Fact]
        public void CreateHistogram_WhenNotEnabled_ReturnsNull()
        {
            var histogram = _foundry.CreateHistogram<double>("custom.histogram");

            Assert.Null(histogram);
        }

        [Fact]
        public void CreateHistogram_WithNullFoundry_ThrowsArgumentNullException()
        {
            IWorkflowFoundry? foundry = null;

            Assert.Throws<ArgumentNullException>(() => foundry!.CreateHistogram<double>("histogram"));
        }
    }
}
