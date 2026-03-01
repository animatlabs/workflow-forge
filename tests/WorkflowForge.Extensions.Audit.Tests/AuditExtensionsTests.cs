using System;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Audit.Options;
using WF = WorkflowForge;

namespace WorkflowForge.Extensions.Audit.Tests
{
    public class AuditExtensionsShould : IDisposable
    {
        private readonly IWorkflowFoundry _foundry;
        private readonly InMemoryAuditProvider _auditProvider;
        private readonly ISystemTimeProvider _timeProvider;

        public AuditExtensionsShould()
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
        public void AddMiddleware_GivenUseAudit()
        {
            var options = new AuditMiddlewareOptions { DetailLevel = AuditDetailLevel.Verbose };
            var result = _foundry.UseAudit(_auditProvider, options, _timeProvider, "test-user");

            Assert.Same(_foundry, result);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenUseAuditWithNullFoundry()
        {
            IWorkflowFoundry? nullFoundry = null;

            Assert.Throws<ArgumentNullException>(() =>
                nullFoundry!.UseAudit(_auditProvider));
        }

        [Fact]
        public void ThrowArgumentNullException_GivenUseAuditWithNullAuditProvider()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _foundry.UseAudit(null!));
        }

        [Fact]
        public async Task CreateAuditEntry_GivenWriteCustomAuditAsync()
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
        public async Task ThrowArgumentNullException_GivenWriteCustomAuditAsyncWithNullFoundry()
        {
            IWorkflowFoundry? nullFoundry = null;

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                nullFoundry!.WriteCustomAuditAsync(_auditProvider, "Op", AuditEventType.Custom, "Status"));
        }

        [Fact]
        public async Task ThrowArgumentNullException_GivenWriteCustomAuditAsyncWithNullProvider()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _foundry.WriteCustomAuditAsync(null!, "Op", AuditEventType.Custom, "Status"));
        }

        [Fact]
        public async Task ThrowArgumentException_GivenWriteCustomAuditAsyncWithEmptyOperationName()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _foundry.WriteCustomAuditAsync(_auditProvider, "", AuditEventType.Custom, "Status"));
        }

        [Fact]
        public async Task ThrowArgumentException_GivenWriteCustomAuditAsyncWithNullOperationName()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _foundry.WriteCustomAuditAsync(_auditProvider, null!, AuditEventType.Custom, "Status"));
        }

        [Fact]
        public async Task UseUnknown_GivenWriteCustomAuditAsyncWithoutWorkflowName()
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