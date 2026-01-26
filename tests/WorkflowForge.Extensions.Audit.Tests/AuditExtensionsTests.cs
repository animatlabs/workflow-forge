using System;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Audit.Options;
using WF = WorkflowForge;

namespace WorkflowForge.Extensions.Audit.Tests
{
    public class AuditExtensionsTests : IDisposable
    {
        private readonly IWorkflowFoundry _foundry;
        private readonly InMemoryAuditProvider _auditProvider;
        private readonly ISystemTimeProvider _timeProvider;

        public AuditExtensionsTests()
        {
            _timeProvider = SystemTimeProvider.Instance;
            _foundry = WF.WorkflowForge.CreateFoundry("AuditExtTest");
            _foundry.Properties["Workflow.Name"] = "TestWorkflow";
            _auditProvider = new InMemoryAuditProvider();
        }

        public void Dispose()
        {
            (_foundry as IDisposable)?.Dispose();
        }

        [Fact]
        public void UseAudit_ShouldAddMiddleware()
        {
            var options = new AuditMiddlewareOptions { DetailLevel = AuditDetailLevel.Verbose };
            var result = _foundry.UseAudit(_auditProvider, options, _timeProvider, "test-user");

            Assert.Same(_foundry, result);
        }

        [Fact]
        public void UseAudit_WithNullFoundry_ShouldThrowArgumentNullException()
        {
            IWorkflowFoundry? nullFoundry = null;

            Assert.Throws<ArgumentNullException>(() =>
                nullFoundry!.UseAudit(_auditProvider));
        }

        [Fact]
        public void UseAudit_WithNullAuditProvider_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _foundry.UseAudit(null!));
        }

        [Fact]
        public async Task WriteCustomAuditAsync_ShouldCreateAuditEntry()
        {
            await _foundry.WriteCustomAuditAsync(
                _auditProvider,
                "CustomOperation",
                AuditEventType.Custom,
                "CustomStatus",
                _timeProvider,
                "custom-user");

            Assert.Single(_auditProvider.Entries);
            var entry = _auditProvider.Entries[0];
            Assert.Equal("CustomOperation", entry.OperationName);
            Assert.Equal(AuditEventType.Custom, entry.EventType);
            Assert.Equal("CustomStatus", entry.Status);
            Assert.Equal("custom-user", entry.InitiatedBy);
            Assert.Equal("TestWorkflow", entry.WorkflowName);
        }

        [Fact]
        public async Task WriteCustomAuditAsync_WithNullFoundry_ShouldThrowArgumentNullException()
        {
            IWorkflowFoundry? nullFoundry = null;

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                nullFoundry!.WriteCustomAuditAsync(_auditProvider, "Op", AuditEventType.Custom, "Status"));
        }

        [Fact]
        public async Task WriteCustomAuditAsync_WithNullProvider_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _foundry.WriteCustomAuditAsync(null!, "Op", AuditEventType.Custom, "Status"));
        }

        [Fact]
        public async Task WriteCustomAuditAsync_WithEmptyOperationName_ShouldThrowArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _foundry.WriteCustomAuditAsync(_auditProvider, "", AuditEventType.Custom, "Status"));
        }

        [Fact]
        public async Task WriteCustomAuditAsync_WithNullOperationName_ShouldThrowArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _foundry.WriteCustomAuditAsync(_auditProvider, null!, AuditEventType.Custom, "Status"));
        }

        [Fact]
        public async Task WriteCustomAuditAsync_WithoutWorkflowName_ShouldUseUnknown()
        {
            var foundryWithoutName = WF.WorkflowForge.CreateFoundry("Test");

            await foundryWithoutName.WriteCustomAuditAsync(
                _auditProvider,
                "TestOp",
                AuditEventType.Custom,
                "Status");

            Assert.Single(_auditProvider.Entries);
            Assert.Equal("Unknown", _auditProvider.Entries[0].WorkflowName);
        }
    }
}