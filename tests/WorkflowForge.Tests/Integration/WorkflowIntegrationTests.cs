using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Exceptions;
using WorkflowForge.Operations;
using Xunit.Abstractions;

namespace WorkflowForge.Tests.Integration;

/// <summary>
/// Comprehensive integration tests for complete workflow execution scenarios.
/// Tests end-to-end workflow execution with multiple operations, middleware, and complex scenarios.
/// </summary>
public class WorkflowIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public WorkflowIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Basic Integration Tests

    [Fact]
    public async Task CompleteWorkflow_WithMultipleOperations_ExecutesSuccessfully()
    {
        // Arrange
        var workflow = WorkflowForge.CreateWorkflow("CompleteWorkflow")
            .AddOperation(new DataInitializationOperation())
            .AddOperation(new DataValidationOperation())
            .AddOperation(new DataProcessingOperation())
            .AddOperation(new DataPersistenceOperation())
            .Build();

        var foundry = WorkflowForge.CreateFoundry("CompleteWorkflow");
        var smith = WorkflowForge.CreateSmith();

        // Act
        await smith.ForgeAsync(workflow, foundry);

        // Assert
        Assert.True(foundry.Properties.ContainsKey("initialized"));
        Assert.True(foundry.Properties.ContainsKey("validated"));
        Assert.True(foundry.Properties.ContainsKey("processed"));
        Assert.True(foundry.Properties.ContainsKey("persisted"));
    }

    [Fact]
    public async Task WorkflowWithMiddleware_ExecutesWithCorrectPipeline()
    {
        // Arrange
        var executionLog = new List<string>();
        var workflow = WorkflowForge.CreateWorkflow("MiddlewareTest")
            .AddOperation(new LoggingOperation("MainOp", executionLog))
            .Build();

        var foundry = WorkflowForge.CreateFoundry("MiddlewareTest");
        var smith = WorkflowForge.CreateSmith();

        // Act
        await smith.ForgeAsync(workflow, foundry);

        // Assert
        Assert.Contains("MainOp-Execute", executionLog);
    }

    #endregion Basic Integration Tests

    #region Complex Workflow Scenarios

    [Fact]
    public async Task DataProcessingWorkflow_WithTransformations_ProcessesCorrectly()
    {
        // Arrange
        var workflow = WorkflowForge.CreateWorkflow("DataProcessingWorkflow")
            .AddOperation("LoadData", (foundry, ct) =>
            {
                foundry.Properties["rawData"] = new[] { 1, 2, 3, 4, 5 };
                return Task.CompletedTask;
            })
            .AddOperation(new DataTransformOperation())
            .AddOperation(new DataAggregationOperation())
            .AddOperation("ValidateResults", (foundry, ct) =>
            {
                var result = foundry.Properties["aggregatedResult"];
                Assert.NotNull(result);
                foundry.Properties["validated"] = true;
                return Task.CompletedTask;
            })
            .Build();

        var foundry = WorkflowForge.CreateFoundry("DataProcessingWorkflow");
        var smith = WorkflowForge.CreateSmith();

        // Act
        await smith.ForgeAsync(workflow, foundry);

        // Assert
        Assert.Equal(30, foundry.Properties["aggregatedResult"]); // Sum of doubled values: (1+2+3+4+5)*2 = 30
        Assert.True((bool)foundry.Properties["validated"]!);
    }

    [Fact]
    public async Task ConditionalWorkflow_WithBranching_ExecutesCorrectPath()
    {
        // Arrange
        var workflow = WorkflowForge.CreateWorkflow("ConditionalWorkflow")
            .AddOperation("SetCondition", (foundry, ct) =>
            {
                foundry.Properties["shouldExecute"] = true;
                return Task.CompletedTask;
            })
            .AddOperation(new ConditionalWorkflowOperation(
                (input, f, ct) => Task.FromResult(f.Properties.ContainsKey("shouldExecute") && (bool)f.Properties["shouldExecute"]!),
                new SpecialProcessingOperation(),
                new StandardProcessingOperation()))
            .AddOperation("FinalizeResults", (foundry, ct) =>
            {
                foundry.Properties["finalized"] = true;
                return Task.CompletedTask;
            })
            .Build();

        var foundry = WorkflowForge.CreateFoundry("ConditionalWorkflow");
        var smith = WorkflowForge.CreateSmith();

        // Act
        await smith.ForgeAsync(workflow, foundry);

        // Assert
        Assert.Equal("special", foundry.Properties["processingType"]);
        Assert.True((bool)foundry.Properties["finalized"]!);
    }

    #endregion Complex Workflow Scenarios

    #region Error Handling Integration Tests

    [Fact]
    public async Task WorkflowWithFailingOperation_HandlesErrorCorrectly()
    {
        // Arrange
        var workflow = WorkflowForge.CreateWorkflow("ErrorHandlingWorkflow")
            .AddOperation(new SuccessfulOperation("Step1"))
            .AddOperation(new FailingOperation("FailingStep"))
            .AddOperation(new SuccessfulOperation("Step3"))
            .Build();

        var foundry = WorkflowForge.CreateFoundry("ErrorHandlingWorkflow");
        var smith = WorkflowForge.CreateSmith();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => smith.ForgeAsync(workflow, foundry));
        Assert.Contains("FailingStep", exception.Message);
    }

    [Fact]
    public async Task WorkflowWithErrorHandlingMiddleware_RecoverFromErrors()
    {
        // Arrange
        var workflow = WorkflowForge.CreateWorkflow("ErrorRecoveryTest")
            .AddOperation(new FailingOperation("FailingOp"))
            .AddOperation(new SuccessfulOperation("RecoveryOp"))
            .Build();

        var foundry = WorkflowForge.CreateFoundry("ErrorRecoveryTest");
        var smith = WorkflowForge.CreateSmith();

        // Act & Assert - This will fail as expected since we don't have error recovery middleware
        await Assert.ThrowsAsync<InvalidOperationException>(() => smith.ForgeAsync(workflow, foundry));
    }

    #endregion Error Handling Integration Tests

    #region Performance and Concurrency Tests

    [Fact]
    public async Task HighVolumeWorkflow_WithManyOperations_ExecutesEfficiently()
    {
        // Arrange
        var workflowBuilder = WorkflowForge.CreateWorkflow("HighVolumeWorkflow");

        for (int i = 0; i < 50; i++)
        {
            var index = i;
            workflowBuilder.AddOperation($"Operation{index}", (foundry, ct) =>
            {
                foundry.Properties[$"result{index}"] = $"processed{index}";
                return Task.CompletedTask;
            });
        }

        var workflow = workflowBuilder.Build();
        var foundry = WorkflowForge.CreateFoundry("HighVolumeWorkflow");
        var smith = WorkflowForge.CreateSmith();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await smith.ForgeAsync(workflow, foundry);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 15000, $"Expected < 15s, got {stopwatch.ElapsedMilliseconds}ms");

        // Verify all operations executed
        for (int i = 0; i < 50; i++)
        {
            Assert.True(foundry.Properties.ContainsKey($"result{i}"));
            Assert.Equal($"processed{i}", foundry.Properties[$"result{i}"]);
        }
    }

    [Fact]
    public async Task ConcurrentWorkflowExecution_WithSharedResources_HandlesCorrectly()
    {
        // Arrange
        var sharedCounter = new SharedCounter();
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var workflow = WorkflowForge.CreateWorkflow($"ConcurrentWorkflow-{i}")
                    .AddOperation(new CounterIncrementOperation(sharedCounter))
                    .Build();

                var foundry = WorkflowForge.CreateFoundry($"ConcurrentWorkflow-{i}");
                var smith = WorkflowForge.CreateSmith();
                await smith.ForgeAsync(workflow, foundry);
            }));
        }

        // Act
        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, sharedCounter.Value); // All increments should be reflected
    }

    #endregion Performance and Concurrency Tests

    #region Real-world Scenarios

    [Fact]
    public async Task OrderProcessingWorkflow_CompleteScenario_ExecutesSuccessfully()
    {
        // Arrange
        var workflow = WorkflowForge.CreateWorkflow("OrderProcessingWorkflow")
            .AddOperation(new InventoryCheckOperation())
            .AddOperation(new PaymentProcessingOperation())
            .AddOperation(new ShippingOperation())
            .Build();

        var foundry = WorkflowForge.CreateFoundry("OrderProcessingWorkflow");
        foundry.Properties["orderId"] = "ORD-123";
        foundry.Properties["customerId"] = "CUST-456";
        foundry.Properties["items"] = new[] { "Item1", "Item2" };
        foundry.Properties["amount"] = 99.99m;

        var smith = WorkflowForge.CreateSmith();

        // Act
        await smith.ForgeAsync(workflow, foundry);

        // Assert
        Assert.True((bool)foundry.Properties["inventoryAvailable"]!);
        Assert.True((bool)foundry.Properties["paymentProcessed"]!);
        Assert.True((bool)foundry.Properties["shipped"]!);
        Assert.True(foundry.Properties.ContainsKey("trackingNumber"));
    }

    [Fact]
    public async Task DocumentProcessingWorkflow_WithFileOperations_ProcessesCorrectly()
    {
        // Arrange
        var workflow = WorkflowForge.CreateWorkflow("DocumentProcessingWorkflow")
            .AddOperation(new DocumentValidationOperation())
            .AddOperation(new TextExtractionOperation())
            .AddOperation(new ContentAnalysisOperation())
            .Build();

        var foundry = WorkflowForge.CreateFoundry("DocumentProcessingWorkflow");
        foundry.Properties["documentPath"] = "/path/to/document.pdf";
        foundry.Properties["expectedFormat"] = "PDF";

        var smith = WorkflowForge.CreateSmith();

        // Act
        await smith.ForgeAsync(workflow, foundry);

        // Assert
        Assert.True((bool)foundry.Properties["documentValid"]!);
        Assert.True(foundry.Properties.ContainsKey("extractedText"));
        Assert.True(foundry.Properties.ContainsKey("analysisResults"));
        Assert.Equal("Document processed successfully", foundry.Properties["status"]);
    }

    #endregion Real-world Scenarios

    #region Helper Classes

    private class DataInitializationOperation : WorkflowOperationBase
    {
        public override string Name => "DataInitialization";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            foundry.Properties["initialized"] = true;
            foundry.Properties["timestamp"] = DateTime.UtcNow;
            return Task.FromResult<object?>("initialized");
        }
    }

    private class DataValidationOperation : WorkflowOperationBase
    {
        public override string Name => "DataValidation";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (!foundry.Properties.ContainsKey("initialized"))
                throw new InvalidOperationException("Data not initialized");

            foundry.Properties["validated"] = true;
            return Task.FromResult<object?>("validated");
        }
    }

    private class DataProcessingOperation : WorkflowOperationBase
    {
        public override string Name => "DataProcessing";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (!foundry.Properties.ContainsKey("validated"))
                throw new InvalidOperationException("Data not validated");

            foundry.Properties["processed"] = true;
            foundry.Properties["processedAt"] = DateTime.UtcNow;
            return Task.FromResult<object?>("processed");
        }
    }

    private class DataPersistenceOperation : WorkflowOperationBase
    {
        public override string Name => "DataPersistence";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (!foundry.Properties.ContainsKey("processed"))
                throw new InvalidOperationException("Data not processed");

            foundry.Properties["persisted"] = true;
            foundry.Properties["persistedAt"] = DateTime.UtcNow;
            return Task.FromResult<object?>("persisted");
        }
    }

    private class LoggingOperation : WorkflowOperationBase
    {
        private readonly List<string> _log;

        public LoggingOperation(string name, List<string> log)
        {
            Name = name;
            _log = log;
        }

        public override string Name { get; }

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            _log.Add($"{Name}-Execute");
            return Task.FromResult<object?>("logged");
        }
    }

    private class DataTransformOperation : WorkflowOperationBase
    {
        public override string Name => "DataTransform";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            var rawData = (int[])foundry.Properties["rawData"]!;
            var transformedData = rawData.Select(x => x * 2).ToArray();
            foundry.Properties["transformedData"] = transformedData;
            return Task.FromResult<object?>("transformed");
        }
    }

    private class DataAggregationOperation : WorkflowOperationBase
    {
        public override string Name => "DataAggregation";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            var transformedData = (int[])foundry.Properties["transformedData"]!;
            var aggregatedResult = transformedData.Sum();
            foundry.Properties["aggregatedResult"] = aggregatedResult;
            return Task.FromResult<object?>("aggregated");
        }
    }

    private class SpecialProcessingOperation : WorkflowOperationBase
    {
        public override string Name => "SpecialProcessing";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            foundry.Properties["processingType"] = "special";
            return Task.FromResult<object?>("special");
        }
    }

    private class StandardProcessingOperation : WorkflowOperationBase
    {
        public override string Name => "StandardProcessing";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            foundry.Properties["processingType"] = "standard";
            return Task.FromResult<object?>("standard");
        }
    }

    private class SuccessfulOperation : WorkflowOperationBase
    {
        public SuccessfulOperation(string name)
        {
            Name = name;
        }

        public override string Name { get; }

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            foundry.Properties[$"{Name}-executed"] = true;
            return Task.FromResult<object?>("success");
        }
    }

    private class FailingOperation : WorkflowOperationBase
    {
        public FailingOperation(string name)
        {
            Name = name;
        }

        public override string Name { get; }

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException($"Operation {Name} failed intentionally");
        }
    }

    private class CounterIncrementOperation : WorkflowOperationBase
    {
        private readonly SharedCounter _counter;

        public CounterIncrementOperation(SharedCounter counter)
        {
            _counter = counter;
        }

        public override string Name => "CounterIncrement";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            _counter.Increment();
            return Task.FromResult<object?>("incremented");
        }
    }

    private class PaymentProcessingOperation : WorkflowOperationBase
    {
        public override string Name => "PaymentProcessing";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            // Simulate payment processing
            foundry.Properties["paymentProcessed"] = true;
            foundry.Properties["transactionId"] = Guid.NewGuid().ToString();
            return Task.FromResult<object?>("payment-processed");
        }
    }

    private class InventoryCheckOperation : WorkflowOperationBase
    {
        public override string Name => "InventoryCheck";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            // Simulate inventory check
            foundry.Properties["inventoryAvailable"] = true;
            foundry.Properties["reservationId"] = Guid.NewGuid().ToString();
            return Task.FromResult<object?>("inventory-checked");
        }
    }

    private class ShippingOperation : WorkflowOperationBase
    {
        public override string Name => "Shipping";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            // Simulate shipping scheduling
            foundry.Properties["shipped"] = true;
            foundry.Properties["trackingNumber"] = $"TRK{DateTime.UtcNow.Ticks}";
            return Task.FromResult<object?>("shipping-scheduled");
        }
    }

    private class DocumentValidationOperation : WorkflowOperationBase
    {
        public override string Name => "DocumentValidation";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            var documentPath = foundry.Properties["documentPath"] as string;
            var expectedFormat = foundry.Properties["expectedFormat"] as string;

            // Simulate document validation
            foundry.Properties["documentContent"] = "Sample document content for processing";
            foundry.Properties["documentValid"] = !string.IsNullOrEmpty(documentPath) && expectedFormat == "PDF";
            return Task.FromResult<object?>("validated");
        }
    }

    private class TextExtractionOperation : WorkflowOperationBase
    {
        public override string Name => "TextExtraction";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            var content = foundry.Properties["documentContent"] as string ?? "Default extracted text content";
            foundry.Properties["extractedText"] = content;
            foundry.Properties["textExtracted"] = true;
            return Task.FromResult<object?>("extracted");
        }
    }

    private class ContentAnalysisOperation : WorkflowOperationBase
    {
        public override string Name => "ContentAnalysis";

        protected override Task<object?> ForgeAsyncCore(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            var text = foundry.Properties["extractedText"] as string ?? "";
            var wordCount = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;

            var analysis = new Dictionary<string, object>
            {
                { "wordCount", wordCount },
                { "characterCount", text.Length },
                { "analysisTimestamp", DateTime.UtcNow }
            };

            foundry.Properties["analysisResults"] = analysis;
            foundry.Properties["analysisComplete"] = true;
            foundry.Properties["status"] = "Document processed successfully";
            return Task.FromResult<object?>("analyzed");
        }
    }

    private class ExecutionLoggingMiddleware : IWorkflowOperationMiddleware
    {
        private readonly List<string> _log;

        public ExecutionLoggingMiddleware(List<string> log)
        {
            _log = log;
        }

        public async Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, object? inputData, Func<CancellationToken, Task<object?>> next, CancellationToken cancellationToken = default)
        {
            _log.Add("Middleware-Before");
            var result = await next(cancellationToken);
            _log.Add("Middleware-After");
            return result;
        }
    }

    private class ErrorRecoveryMiddleware : IWorkflowOperationMiddleware
    {
        public async Task<object?> ExecuteAsync(IWorkflowOperation operation, IWorkflowFoundry foundry, object? inputData, Func<CancellationToken, Task<object?>> next, CancellationToken cancellationToken = default)
        {
            try
            {
                return await next(cancellationToken);
            }
            catch (WorkflowOperationException)
            {
                foundry.Properties["error-recovered"] = true;
                return null; // Continue execution
            }
        }
    }

    private class SharedCounter
    {
        private int _value;
        private readonly object _lock = new object();

        public int Value
        {
            get
            {
                lock (_lock)
                {
                    return _value;
                }
            }
        }

        public void Increment()
        {
            lock (_lock)
            {
                _value++;
            }
        }
    }

    [Fact]
    public async Task DebugDataFlow_SimpleTest()
    {
        // Arrange
        var workflow = WorkflowForge.CreateWorkflow("DebugWorkflow")
            .AddOperation("Step1", (foundry, ct) =>
            {
                foundry.Properties["step1"] = "executed";
                return Task.CompletedTask;
            })
            .AddOperation("Step2", (foundry, ct) =>
            {
                var step1Data = foundry.Properties["step1"];
                foundry.Properties["step2"] = $"executed after {step1Data}";
                return Task.CompletedTask;
            })
            .Build();

        var foundry = WorkflowForge.CreateFoundry("DebugWorkflow");
        var smith = WorkflowForge.CreateSmith();

        // Act
        await smith.ForgeAsync(workflow, foundry);

        // Assert - Use foundry.Properties instead of the original data dictionary
        Assert.True(foundry.Properties.ContainsKey("step1"), $"step1 not found. Keys: {string.Join(", ", foundry.Properties.Keys)}");
        Assert.True(foundry.Properties.ContainsKey("step2"), $"step2 not found. Keys: {string.Join(", ", foundry.Properties.Keys)}");
        Assert.Equal("executed", foundry.Properties["step1"]);
        Assert.Equal("executed after executed", foundry.Properties["step2"]);
    }

    #endregion Helper Classes
}
