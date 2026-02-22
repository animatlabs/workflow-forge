using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Audit;
using WorkflowForge.Extensions.Audit.Options;
using WorkflowForge.Operations;
using WF = WorkflowForge;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates audit logging capabilities using WorkflowForge.Extensions.Audit
/// </summary>
public class AuditSample : ISample
{
    public string Name => "Audit Extension";
    public string Description => "Demonstrates comprehensive audit logging and compliance tracking";

    public async Task RunAsync()
    {
        Console.WriteLine("WorkflowForge Audit Extension Sample");
        Console.WriteLine("=====================================");
        Console.WriteLine();

        // Sample 1: Basic Audit Logging
        await RunBasicAudit();
        Console.WriteLine();

        // Sample 2: Audit with User Context
        await RunAuditWithUserContext();
        Console.WriteLine();

        // Sample 3: Audit with Metadata
        await RunAuditWithMetadata();
        Console.WriteLine();

        // Sample 4: Custom Audit Entries
        await RunCustomAuditEntries();
    }

    private async Task RunBasicAudit()
    {
        Console.WriteLine("1. Basic Audit Logging");
        Console.WriteLine("   -------------------");

        // Create in-memory audit provider
        var auditProvider = new InMemoryAuditProvider();

        using var foundry = WF.WorkflowForge.CreateFoundry("BasicAudit");
        foundry.Properties["Workflow.Name"] = "OrderProcessing";

        // Enable audit logging
        foundry.UseAudit(auditProvider);

        // Create simple workflow
        var workflow = WF.WorkflowForge.CreateWorkflow()
            .WithName("ProcessOrder")
            .AddOperation(new ValidateOperation())
            .AddOperation(new ProcessOperation())
            .AddOperation(new CompleteOperation())
            .Build();

        using var smith = WF.WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        // Display audit log
        Console.WriteLine($"   Audit Entries Created: {auditProvider.Entries.Count}");
        foreach (var entry in auditProvider.Entries)
        {
            Console.WriteLine($"   [{entry.Timestamp:HH:mm:ss.fff}] {entry.EventType} - {entry.OperationName}: {entry.Status}");
        }
    }

    private async Task RunAuditWithUserContext()
    {
        Console.WriteLine("2. Audit with User Context");
        Console.WriteLine("   -----------------------");

        var auditProvider = new InMemoryAuditProvider();

        using var foundry = WF.WorkflowForge.CreateFoundry("UserContextAudit");
        foundry.Properties["Workflow.Name"] = "PaymentProcessing";

        // Enable audit with user context
        var options = new AuditMiddlewareOptions { DetailLevel = AuditDetailLevel.Standard };
        foundry.UseAudit(
            auditProvider,
            options,
            initiatedBy: "admin@company.com");

        var workflow = WF.WorkflowForge.CreateWorkflow()
            .WithName("ProcessPayment")
            .AddOperation(new ValidatePaymentOperation())
            .AddOperation(new ChargePaymentOperation())
            .Build();

        using var smith = WF.WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        Console.WriteLine($"   Total Audit Entries: {auditProvider.Entries.Count}");
        Console.WriteLine($"   Initiated By: {auditProvider.Entries.First().InitiatedBy}");

        var successfulOps = auditProvider.Entries.Count(e => e.Status == "Completed");
        Console.WriteLine($"   Successful Operations: {successfulOps}");
    }

    private async Task RunAuditWithMetadata()
    {
        Console.WriteLine("3. Audit with Metadata");
        Console.WriteLine("   --------------------");

        var auditProvider = new InMemoryAuditProvider();

        using var foundry = WF.WorkflowForge.CreateFoundry("MetadataAudit");
        foundry.Properties["Workflow.Name"] = "AccountCreation";
        foundry.Properties["UserId"] = "USER-12345";
        foundry.Properties["IPAddress"] = "192.168.1.100";
        foundry.Properties["UserAgent"] = "Mozilla/5.0";

        // Enable audit with metadata
        var verboseOptions = new AuditMiddlewareOptions { DetailLevel = AuditDetailLevel.Verbose };
        foundry.UseAudit(
            auditProvider,
            verboseOptions,
            initiatedBy: "system"); // Include all foundry properties

        var workflow = WF.WorkflowForge.CreateWorkflow()
            .WithName("CreateAccount")
            .AddOperation(new ValidateOperation())
            .AddOperation(new CreateAccountOperation())
            .Build();

        using var smith = WF.WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        Console.WriteLine($"   Audit Entries: {auditProvider.Entries.Count}");

        var firstEntry = auditProvider.Entries.First();
        Console.WriteLine($"   Metadata Captured:");
        Console.WriteLine($"     - UserId: {(firstEntry.Metadata.TryGetValue("UserId", out var uid) ? uid : null)}");
        Console.WriteLine($"     - IPAddress: {(firstEntry.Metadata.TryGetValue("IPAddress", out var ip) ? ip : null)}");
        Console.WriteLine($"     - Total Metadata Fields: {firstEntry.Metadata.Count}");
    }

    private async Task RunCustomAuditEntries()
    {
        Console.WriteLine("4. Custom Audit Entries");
        Console.WriteLine("   --------------------");

        var auditProvider = new InMemoryAuditProvider();

        using var foundry = WF.WorkflowForge.CreateFoundry("CustomAudit");
        foundry.Properties["Workflow.Name"] = "DataProcessing";

        // Enable audit
        foundry.UseAudit(auditProvider, initiatedBy: "data-processor");

        // Write custom audit entry before workflow
        await foundry.WriteCustomAuditAsync(
            auditProvider,
            "PreProcessingCheck",
            AuditEventType.Custom,
            "Verified",
            initiatedBy: "validation-service");

        var workflow = WF.WorkflowForge.CreateWorkflow()
            .WithName("ProcessData")
            .AddOperation(new ProcessDataOperation())
            .Build();

        using var smith = WF.WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        // Write custom audit entry after workflow
        await foundry.WriteCustomAuditAsync(
            auditProvider,
            "PostProcessingCheck",
            AuditEventType.Custom,
            "Completed",
            initiatedBy: "verification-service");

        Console.WriteLine($"   Total Audit Entries: {auditProvider.Entries.Count}");
        Console.WriteLine($"   Custom Entries: {auditProvider.Entries.Count(e => e.EventType == AuditEventType.Custom)}");
        Console.WriteLine($"   Operation Entries: {auditProvider.Entries.Count(e => e.EventType == AuditEventType.OperationStarted || e.EventType == AuditEventType.OperationCompleted)}");

        Console.WriteLine();
        Console.WriteLine("   Audit Timeline:");
        foreach (var entry in auditProvider.Entries.OrderBy(e => e.Timestamp))
        {
            var duration = entry.DurationMs.HasValue ? $" ({entry.DurationMs}ms)" : "";
            Console.WriteLine($"     [{entry.Timestamp:HH:mm:ss.fff}] {entry.OperationName} - {entry.Status}{duration}");
        }
    }

    // Sample Operations
    public class ValidateOperation : WorkflowOperationBase
    {
        public override string Name => "Validate";

        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken); // Simulate work
            foundry.Logger.LogInformation("Validation completed");
            return "Validated";
        }
    }

    public class ProcessOperation : WorkflowOperationBase
    {
        public override string Name => "Process";

        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            await Task.Delay(20, cancellationToken); // Simulate work
            foundry.Logger.LogInformation("Processing completed");
            return "Processed";
        }
    }

    public class CompleteOperation : WorkflowOperationBase
    {
        public override string Name => "Complete";

        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken); // Simulate work
            foundry.Logger.LogInformation("Completion confirmed");
            return "Completed";
        }
    }

    public class ValidatePaymentOperation : WorkflowOperationBase
    {
        public override string Name => "ValidatePayment";

        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            await Task.Delay(15, cancellationToken);
            return "Payment validated";
        }
    }

    public class ChargePaymentOperation : WorkflowOperationBase
    {
        public override string Name => "ChargePayment";

        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            await Task.Delay(25, cancellationToken);
            return "Payment charged";
        }
    }

    public class CreateAccountOperation : WorkflowOperationBase
    {
        public override string Name => "CreateAccount";

        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            await Task.Delay(30, cancellationToken);
            return "Account created";
        }
    }

    public class ProcessDataOperation : WorkflowOperationBase
    {
        public override string Name => "ProcessData";

        protected override async Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            await Task.Delay(20, cancellationToken);
            return "Data processed";
        }
    }
}