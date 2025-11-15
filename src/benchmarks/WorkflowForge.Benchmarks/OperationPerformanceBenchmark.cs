using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Benchmarks;

/// <summary>
/// Benchmarks individual operation performance:
/// - Built-in operation types
/// - Custom operation patterns
/// - Operation creation overhead
/// - Restoration/compensation performance
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, iterationCount: 25)]
[MarkdownExporter]
[HtmlExporter]
public class OperationPerformanceBenchmark
{
    private IWorkflowFoundry _foundry = null!;

    [GlobalSetup]
    public void Setup()
    {
        _foundry = WorkflowForge.CreateFoundry("BenchmarkFoundry");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _foundry?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public async Task<object?> DelegateOperationExecution()
    {
        var operation = new DelegateWorkflowOperation("TestDelegate", async (input, foundry, token) =>
        {
            await Task.Yield();
            return "Delegate result";
        });

        return await operation.ForgeAsync(null, _foundry);
    }

    [Benchmark]
    public async Task<object?> ActionOperationExecution()
    {
        var operation = new ActionWorkflowOperation("TestAction", async (input, foundry, token) =>
        {
            await Task.Yield();
            foundry.Properties["action_executed"] = true;
        });

        return await operation.ForgeAsync(null, _foundry);
    }

    [Benchmark]
    public async Task<object?> CustomOperationExecution()
    {
        var operation = new FastBenchmarkOperation();
        return await operation.ForgeAsync(null, _foundry);
    }

    [Benchmark]
    public async Task<object?> LoggingOperationExecution()
    {
        var operation = new LoggingOperation("Benchmark log message", WorkflowForgeLogLevel.Information);
        return await operation.ForgeAsync(null, _foundry);
    }

    [Benchmark]
    public async Task<object?> DelayOperationExecution()
    {
        var operation = new DelayOperation(TimeSpan.FromMilliseconds(1));
        return await operation.ForgeAsync(null, _foundry);
    }

    [Benchmark]
    public async Task<object?> ConditionalOperationTrue()
    {
        var operation = new ConditionalWorkflowOperation(
            (input, foundry, token) => Task.FromResult(true),
            new DelegateWorkflowOperation("ThenOp", async (input, foundry, token) =>
            {
                await Task.Yield();
                return "Then executed";
            })
        );

        return await operation.ForgeAsync(null, _foundry);
    }

    [Benchmark]
    public async Task<object?> ConditionalOperationFalse()
    {
        var operation = new ConditionalWorkflowOperation(
            (input, foundry, token) => Task.FromResult(false),
            new DelegateWorkflowOperation("ThenOp", async (input, foundry, token) =>
            {
                await Task.Yield();
                return "Then executed";
            }),
            new DelegateWorkflowOperation("ElseOp", async (input, foundry, token) =>
            {
                await Task.Yield();
                return "Else executed";
            })
        );

        return await operation.ForgeAsync(null, _foundry);
    }

    [Benchmark]
    public async Task<object?> ForEachOperationSmallCollection()
    {
        var items = new[] { "Item1", "Item2", "Item3" };
        var operation = new ForEachWorkflowOperation(
            new[] { new FastBenchmarkOperation() },
            TimeSpan.FromSeconds(1),
            ForEachDataStrategy.SharedInput,
            null,
            "ProcessSmallItems"
        );

        return await operation.ForgeAsync(items, _foundry);
    }

    [Benchmark]
    public async Task<object?> ForEachOperationLargeCollection()
    {
        var items = Enumerable.Range(1, 100).Select(i => $"Item{i}").ToArray();
        var operation = new ForEachWorkflowOperation(
            new[] { new FastBenchmarkOperation() },
            TimeSpan.FromSeconds(30),
            ForEachDataStrategy.SharedInput,
            null,
            "ProcessLargeItems"
        );

        return await operation.ForgeAsync(items, _foundry);
    }

    [Benchmark]
    public IWorkflowOperation DelegateOperationCreation()
    {
        return new DelegateWorkflowOperation("CreationTest", async (input, foundry, token) =>
        {
            await Task.Yield();
            return "Created";
        });
    }

    [Benchmark]
    public IWorkflowOperation ActionOperationCreation()
    {
        return new ActionWorkflowOperation("CreationTest", async (input, foundry, token) =>
        {
            await Task.Yield();
        });
    }

    [Benchmark]
    public IWorkflowOperation CustomOperationCreation()
    {
        return new FastBenchmarkOperation();
    }

    [Benchmark]
    public async Task<string> OperationWithRestoration()
    {
        var operation = new RestorationTestOperation();

        // Execute operation
        await operation.ForgeAsync(null, _foundry);

        // Perform restoration
        await operation.RestoreAsync("test_context", _foundry);

        return "Restoration completed";
    }

    [Benchmark]
    public async Task<string> OperationDataManipulation()
    {
        var operation = new DataManipulationOperation();
        await operation.ForgeAsync(null, _foundry);
        return "Data manipulation completed";
    }

    [Benchmark]
    public async Task<string> ChainedOperationsExecution()
    {
        using var foundry = WorkflowForge.CreateFoundry("ChainedBenchmark");

        foundry
            .WithOperation(new FastBenchmarkOperation())
            .WithOperation(new FastBenchmarkOperation())
            .WithOperation(new FastBenchmarkOperation());

        await foundry.ForgeAsync();
        return "Chained operations completed";
    }

    [Benchmark]
    public async Task<string> OperationExceptionHandling()
    {
        var operation = new ExceptionTestOperation();

        try
        {
            await operation.ForgeAsync(null, _foundry);
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }

        return "Exception handled";
    }
}

/// <summary>
/// Fast benchmark operation with minimal overhead
/// </summary>
public class FastBenchmarkOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "FastBenchmark";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return "Fast result";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    { }
}

/// <summary>
/// Operation that tests restoration performance
/// </summary>
public class RestorationTestOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "RestorationTest";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        foundry.Properties["restoration_test"] = "executed";
        return "Executed";
    }

    public async Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        foundry.Properties.TryRemove("restoration_test", out _);
    }

    public void Dispose()
    { }
}

/// <summary>
/// Operation that performs data manipulation
/// </summary>
public class DataManipulationOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "DataManipulation";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        // Simulate data manipulation
        foundry.Properties["data_timestamp"] = DateTime.UtcNow;
        foundry.Properties["data_size"] = 1024;
        foundry.Properties["data_processed"] = true;

        // Simulate some processing time
        for (int i = 0; i < 100; i++)
        {
            foundry.Properties[$"item_{i}"] = $"value_{i}";
        }

        return "Data processed";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    { }
}

/// <summary>
/// Operation that throws exceptions for testing exception handling performance
/// </summary>
public class ExceptionTestOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "ExceptionTest";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        throw new InvalidOperationException("Benchmark exception");
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    { }
}