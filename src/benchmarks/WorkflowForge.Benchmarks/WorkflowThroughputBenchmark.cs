using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using WorkflowForge;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Benchmarks;

/// <summary>
/// Benchmarks WorkflowForge throughput capabilities:
/// - Sequential operation execution
/// - Parallel operation execution
/// - Different operation types
/// - Scaling with operation count
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, iterationCount: 50)]
[MarkdownExporter]
[HtmlExporter]
public class WorkflowThroughputBenchmark
{
    private FoundryConfiguration _minimalConfig = null!;
    private FoundryConfiguration _performanceConfig = null!;

    [Params(1, 5, 10, 25, 50)]
    public int OperationCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _minimalConfig = FoundryConfiguration.Minimal();
        _performanceConfig = FoundryConfiguration.HighPerformance();
    }

    [Benchmark(Baseline = true)]
    public async Task<string> SequentialDelegateOperations()
    {
        using var foundry = WorkflowForge.CreateFoundry("SequentialBenchmark", _minimalConfig);
        
        for (int i = 0; i < OperationCount; i++)
        {
            var operationIndex = i;
            foundry.WithOperation($"Operation{operationIndex}", async (foundry) =>
            {
                // Simulate lightweight work
                await Task.Delay(1);
                foundry.Properties[$"result_{operationIndex}"] = $"Result{operationIndex}";
            });
        }

        await foundry.ForgeAsync();
        return "Completed";
    }

    [Benchmark]
    public async Task<string> SequentialCustomOperations()
    {
        using var foundry = WorkflowForge.CreateFoundry("CustomOpsBenchmark", _minimalConfig);
        
        for (int i = 0; i < OperationCount; i++)
        {
            foundry.WithOperation(new LightweightOperation($"LightOp{i}"));
        }

        await foundry.ForgeAsync();
        return "Completed";
    }

    [Benchmark]
    public async Task<string> HighPerformanceConfiguration()
    {
        using var foundry = WorkflowForge.CreateFoundry("HighPerfBenchmark", _performanceConfig);
        
        for (int i = 0; i < OperationCount; i++)
        {
            var operationIndex = i;
            foundry.WithOperation($"FastOperation{operationIndex}", async (foundry) =>
            {
                // Minimal delay for high-performance scenario
                await Task.Yield();
                foundry.Properties[$"fast_result_{operationIndex}"] = $"FastResult{operationIndex}";
            });
        }

        await foundry.ForgeAsync();
        return "Completed";
    }

    [Benchmark]
    public async Task<string> DataPassingWorkflow()
    {
        using var foundry = WorkflowForge.CreateFoundry("DataPassingBenchmark", _minimalConfig);
        
        foundry.Properties["counter"] = 0;
        
        for (int i = 0; i < OperationCount; i++)
        {
            var operationIndex = i;
            foundry.WithOperation($"DataOp{operationIndex}", async (foundry) =>
            {
                var counter = (int)foundry.Properties["counter"]!;
                foundry.Properties["counter"] = counter + 1;
                foundry.Properties[$"result_{operationIndex}"] = $"Data_{counter}";
                await Task.Delay(1);
            });
        }

        await foundry.ForgeAsync();
        return $"Final counter: {foundry.Properties["counter"]}";
    }

    [Benchmark]
    public async Task<string> ConditionalOperationsWorkflow()
    {
        using var foundry = WorkflowForge.CreateFoundry("ConditionalBenchmark", _minimalConfig);
        
        foundry.Properties["process_count"] = 0;
        
        for (int i = 0; i < OperationCount; i++)
        {
            var operationIndex = i;
            
            // Add conditional operation (50% will execute the "then" branch)
            foundry.WithOperation(new ConditionalWorkflowOperation(
                (inputData, foundry, cancellationToken) => Task.FromResult(operationIndex % 2 == 0),
                new DelegateWorkflowOperation($"ThenOp{operationIndex}", async (inputData, foundry, cancellationToken) =>
                {
                    var count = (int)foundry.Properties["process_count"]!;
                    foundry.Properties["process_count"] = count + 1;
                    await Task.Delay(1, cancellationToken);
                    return $"Then{operationIndex}";
                }),
                new DelegateWorkflowOperation($"ElseOp{operationIndex}", async (inputData, foundry, cancellationToken) =>
                {
                    await Task.Delay(1, cancellationToken);
                    return $"Else{operationIndex}";
                })));
        }

        await foundry.ForgeAsync();
        return $"Processed: {foundry.Properties["process_count"]}";
    }

    [Benchmark]
    public async Task<string> ForEachLoopWorkflow()
    {
        using var foundry = WorkflowForge.CreateFoundry("ForEachBenchmark", _minimalConfig);
        
        // Create a collection to iterate over
        var items = Enumerable.Range(1, OperationCount).Select(i => $"Item{i}").ToArray();
        foundry.Properties["items"] = items;
        
        foundry.WithOperation(new ForEachWorkflowOperation(
            new[] { new LightweightOperation("ProcessItem") },
            TimeSpan.FromSeconds(30),
            ForEachDataStrategy.SharedInput,
            null, // maxConcurrency
            "ProcessAllItems"
        ));

        await foundry.ForgeAsync();
        return $"Processed {items.Length} items";
    }

    [Benchmark]
    public async Task<string> LoggingOperationsWorkflow()
    {
        using var foundry = WorkflowForge.CreateFoundry("LoggingBenchmark", _minimalConfig);
        
        for (int i = 0; i < OperationCount; i++)
        {
            foundry.WithOperation(new LoggingOperation($"Benchmark operation {i}", LogLevel.Information));
            
            var operationIndex = i;
            foundry.WithOperation($"WorkOp{operationIndex}", async (foundry) =>
            {
                await Task.Delay(1);
                foundry.Properties[$"work_result_{operationIndex}"] = $"Work{operationIndex}";
            });
        }

        await foundry.ForgeAsync();
        return "Completed with logging";
    }

    [Benchmark]
    public async Task<string> MemoryIntensiveWorkflow()
    {
        using var foundry = WorkflowForge.CreateFoundry("MemoryBenchmark", _minimalConfig);
        
        for (int i = 0; i < OperationCount; i++)
        {
            var operationIndex = i;
            foundry.WithOperation($"MemoryOp{operationIndex}", async (foundry) =>
            {
                // Simulate memory allocation
                var data = new byte[1024]; // 1KB allocation per operation
                Array.Fill(data, (byte)(operationIndex % 256));
                
                foundry.Properties[$"data_{operationIndex}"] = data;
                await Task.Delay(1);
            });
        }

        await foundry.ForgeAsync();
        return "Memory operations completed";
    }
}

/// <summary>
/// Lightweight operation for benchmarking
/// </summary>
public class LightweightOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public bool SupportsRestore => false;

    public LightweightOperation(string name)
    {
        Name = name;
    }

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        // Minimal work simulation
        await Task.Yield();
        return $"Result from {Name}";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Dispose() { }
} 