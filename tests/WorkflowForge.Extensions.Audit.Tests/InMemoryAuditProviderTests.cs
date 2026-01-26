using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Audit.Tests
{
    public class InMemoryAuditProviderTests
    {
        [Fact]
        public async Task WriteAuditEntryAsync_ShouldStoreEntry()
        {
            var provider = new InMemoryAuditProvider();
            var entry = CreateTestEntry();

            await provider.WriteAuditEntryAsync(entry, CancellationToken.None);

            Assert.Single(provider.Entries);
            Assert.Equal(entry.AuditId, provider.Entries[0].AuditId);
        }

        [Fact]
        public async Task WriteAuditEntryAsync_MultipleEntries_ShouldStoreAll()
        {
            var provider = new InMemoryAuditProvider();
            var entry1 = CreateTestEntry();
            var entry2 = CreateTestEntry();
            var entry3 = CreateTestEntry();

            await provider.WriteAuditEntryAsync(entry1, CancellationToken.None);
            await provider.WriteAuditEntryAsync(entry2, CancellationToken.None);
            await provider.WriteAuditEntryAsync(entry3, CancellationToken.None);

            Assert.Equal(3, provider.Entries.Count);
        }

        [Fact]
        public async Task Clear_ShouldRemoveAllEntries()
        {
            var provider = new InMemoryAuditProvider();
            await provider.WriteAuditEntryAsync(CreateTestEntry(), CancellationToken.None);
            await provider.WriteAuditEntryAsync(CreateTestEntry(), CancellationToken.None);

            Assert.Equal(2, provider.Entries.Count);

            provider.Clear();

            Assert.Empty(provider.Entries);
        }

        [Fact]
        public async Task FlushAsync_ShouldSucceed()
        {
            var provider = new InMemoryAuditProvider();
            await provider.WriteAuditEntryAsync(CreateTestEntry(), CancellationToken.None);

            await provider.FlushAsync(CancellationToken.None);

            Assert.Single(provider.Entries);
        }

        [Fact]
        public async Task Entries_ShouldBeReadOnly()
        {
            var provider = new InMemoryAuditProvider();
            await provider.WriteAuditEntryAsync(CreateTestEntry(), CancellationToken.None);

            var entries = provider.Entries;
            Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<AuditEntry>>(entries);
        }

        [Fact]
        public async Task WriteAuditEntryAsync_ConcurrentWrites_ShouldHandleThreadSafely()
        {
            var provider = new InMemoryAuditProvider();
            var tasks = new Task[100];

            for (int i = 0; i < 100; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    await provider.WriteAuditEntryAsync(CreateTestEntry(), CancellationToken.None);
                });
            }

            await Task.WhenAll(tasks);

            Assert.Equal(100, provider.Entries.Count);
        }

        private AuditEntry CreateTestEntry()
        {
            return new AuditEntry(
                executionId: Guid.NewGuid(),
                workflowName: "TestWorkflow",
                operationName: "TestOperation",
                eventType: AuditEventType.OperationCompleted,
                status: "Completed",
                initiatedBy: "test-user",
                metadata: null,
                errorMessage: null,
                durationMs: 100,
                timestamp: DateTimeOffset.UtcNow);
        }
    }
}