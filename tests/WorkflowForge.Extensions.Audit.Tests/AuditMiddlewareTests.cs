using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WF = WorkflowForge;

namespace WorkflowForge.Extensions.Audit.Tests
{
    public class AuditMiddlewareTests : IDisposable
    {
        private readonly IWorkflowFoundry _foundry;
        private readonly InMemoryAuditProvider _auditProvider;
        private readonly TestOperation _operation;
        private readonly ISystemTimeProvider _timeProvider;

        public AuditMiddlewareTests()
        {
            _timeProvider = SystemTimeProvider.Instance;
            _foundry = WF.WorkflowForge.CreateFoundry("AuditTest");
            _foundry.Properties["Workflow.Name"] = "TestWorkflow";
            _auditProvider = new InMemoryAuditProvider();
            _operation = new TestOperation();
        }

        public void Dispose()
        {
            (_foundry as IDisposable)?.Dispose();
        }

        [Fact]
        public async Task ExecuteAsync_SuccessfulOperation_ShouldCreateStartedAndCompletedEntries()
        {
            var middleware = new AuditMiddleware(_auditProvider, _timeProvider, "test-user", includeMetadata: false);

            var nextCalled = false;
            Task<object?> Next() { nextCalled = true; return Task.FromResult<object?>("result"); }

            var result = await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

            Assert.True(nextCalled);
            Assert.Equal("result", result);
            Assert.Equal(2, _auditProvider.Entries.Count);

            var startedEntry = _auditProvider.Entries.First(e => e.EventType == AuditEventType.OperationStarted);
            Assert.Equal(_operation.Name, startedEntry.OperationName);
            Assert.Equal("TestWorkflow", startedEntry.WorkflowName);
            Assert.Equal("Started", startedEntry.Status);
            Assert.Equal("test-user", startedEntry.InitiatedBy);

            var completedEntry = _auditProvider.Entries.First(e => e.EventType == AuditEventType.OperationCompleted);
            Assert.Equal(_operation.Name, completedEntry.OperationName);
            Assert.Equal("Completed", completedEntry.Status);
            Assert.NotNull(completedEntry.DurationMs);
            Assert.True(completedEntry.DurationMs >= 0);
        }

        [Fact]
        public async Task ExecuteAsync_FailedOperation_ShouldCreateFailedEntry()
        {
            var middleware = new AuditMiddleware(_auditProvider, _timeProvider, "test-user", includeMetadata: false);

            Task<object?> Next() => throw new InvalidOperationException("Test error");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None));

            Assert.Equal(2, _auditProvider.Entries.Count);

            var failedEntry = _auditProvider.Entries.First(e => e.EventType == AuditEventType.OperationFailed);
            Assert.Equal(_operation.Name, failedEntry.OperationName);
            Assert.Equal("Failed", failedEntry.Status);
            Assert.Equal("Test error", failedEntry.ErrorMessage);
            Assert.NotNull(failedEntry.DurationMs);
        }

        [Fact]
        public async Task ExecuteAsync_WithMetadata_ShouldIncludeFoundryProperties()
        {
            _foundry.Properties["CustomProperty"] = "CustomValue";
            var middleware = new AuditMiddleware(_auditProvider, _timeProvider, "test-user", includeMetadata: true);

            Task<object?> Next() => Task.FromResult<object?>(null);

            await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

            var completedEntry = _auditProvider.Entries.First(e => e.EventType == AuditEventType.OperationCompleted);
            Assert.NotEmpty(completedEntry.Metadata);
            Assert.True(completedEntry.Metadata.ContainsKey("CustomProperty"));
            Assert.Equal("CustomValue", completedEntry.Metadata["CustomProperty"]);
        }

        [Fact]
        public async Task ExecuteAsync_WithoutMetadata_ShouldHaveEmptyMetadata()
        {
            _foundry.Properties["CustomProperty"] = "CustomValue";
            var middleware = new AuditMiddleware(_auditProvider, _timeProvider, "test-user", includeMetadata: false);

            Task<object?> Next() => Task.FromResult<object?>(null);

            await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

            var completedEntry = _auditProvider.Entries.First(e => e.EventType == AuditEventType.OperationCompleted);
            Assert.Empty(completedEntry.Metadata);
        }

        [Fact]
        public void Constructor_WithNullAuditProvider_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new AuditMiddleware(null!, _timeProvider));
        }

        [Fact]
        public async Task ExecuteAsync_WithDefaultTimeProvider_ShouldUseSystemTime()
        {
            var middleware = new AuditMiddleware(_auditProvider);

            Task<object?> Next() => Task.FromResult<object?>(null);

            var beforeExecution = DateTimeOffset.UtcNow;
            await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);
            var afterExecution = DateTimeOffset.UtcNow;

            var entry = _auditProvider.Entries.First();
            Assert.True(entry.Timestamp >= beforeExecution);
            Assert.True(entry.Timestamp <= afterExecution);
        }

        private class TestOperation : IWorkflowOperation
        {
            public Guid Id { get; } = Guid.NewGuid();
            public string Name => "TestOperation";
            public bool SupportsRestore => false;

            public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<object?>(null);
            }

            public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            { }
        }
    }
}