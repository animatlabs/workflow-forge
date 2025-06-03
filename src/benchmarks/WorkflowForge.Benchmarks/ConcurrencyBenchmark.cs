using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System.Collections.Concurrent;
using WorkflowForge;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;

namespace WorkflowForge.Benchmarks;

/// <summary>
/// Benchmarks concurrency and parallelism scenarios:
/// - Multiple foundries running concurrently
/// - Thread safety validation
/// - Resource contention scenarios
/// - Scalability under concurrent load
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, iterationCount: 25)]
[MarkdownExporter]
[HtmlExporter]
public class ConcurrencyBenchmark
{
    private FoundryConfiguration _config = null!;

    [Params(1, 2, 4, 8, 16)]
    public int ConcurrentWorkflowCount { get; set; }

    [Params(5, 10, 25)]
    public int OperationsPerWorkflow { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _config = FoundryConfiguration.HighPerformance();
    }

    [Benchmark(Baseline = true)]
    public async Task<string> SequentialWorkflows()
    {
        var results = new List<string>();
        
        for (int i = 0; i < ConcurrentWorkflowCount; i++)
        {
            var result = await RunSingleWorkflow($"Sequential_{i}");
            results.Add(result);
        }
        
        return $"Completed {results.Count} workflows sequentially";
    }

    [Benchmark]
    public async Task<string> ConcurrentWorkflows()
    {
        var tasks = new List<Task<string>>();
        
        for (int i = 0; i < ConcurrentWorkflowCount; i++)
        {
            var workflowIndex = i;
            tasks.Add(Task.Run(async () => await RunSingleWorkflow($"Concurrent_{workflowIndex}")));
        }
        
        var results = await Task.WhenAll(tasks);
        return $"Completed {results.Length} workflows concurrently";
    }

    [Benchmark]
    public async Task<string> ParallelWorkflows()
    {
        var results = new string[ConcurrentWorkflowCount];
        
        await Parallel.ForEachAsync(
            Enumerable.Range(0, ConcurrentWorkflowCount),
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            async (index, cancellationToken) =>
            {
                results[index] = await RunSingleWorkflow($"Parallel_{index}");
            });
        
        return $"Completed {results.Length} workflows in parallel";
    }

    [Benchmark]
    public async Task<string> SharedResourceConcurrency()
    {
        var sharedResource = new SharedBenchmarkResource();
        var tasks = new List<Task<string>>();
        
        for (int i = 0; i < ConcurrentWorkflowCount; i++)
        {
            var workflowIndex = i;
            tasks.Add(Task.Run(async () => await RunWorkflowWithSharedResource($"Shared_{workflowIndex}", sharedResource)));
        }
        
        var results = await Task.WhenAll(tasks);
        return $"Completed {results.Length} workflows with shared resource, final counter: {sharedResource.Counter}";
    }

    [Benchmark]
    public async Task<string> TaskBasedConcurrency()
    {
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
        var tasks = new List<Task<string>>();
        
        for (int i = 0; i < ConcurrentWorkflowCount; i++)
        {
            var workflowIndex = i;
            tasks.Add(ExecuteWithSemaphore(semaphore, () => RunSingleWorkflow($"Semaphore_{workflowIndex}")));
        }
        
        var results = await Task.WhenAll(tasks);
        semaphore.Dispose();
        
        return $"Completed {results.Length} workflows with semaphore control";
    }

    [Benchmark]
    public async Task<string> ConcurrentDataAccess()
    {
        var globalProperties = new ConcurrentDictionary<string, object>();
        var tasks = new List<Task<string>>();
        
        for (int i = 0; i < ConcurrentWorkflowCount; i++)
        {
            var workflowIndex = i;
            tasks.Add(Task.Run(async () => await RunWorkflowWithGlobalData($"DataAccess_{workflowIndex}", globalProperties)));
        }
        
        var results = await Task.WhenAll(tasks);
        return $"Completed {results.Length} workflows with concurrent data access, global entries: {globalProperties.Count}";
    }

    [Benchmark]
    public async Task<string> HighContentionScenario()
    {
        var contentionResource = new HighContentionResource();
        var tasks = new List<Task<string>>();
        
        // Create high contention by having all workflows access the same resource
        for (int i = 0; i < ConcurrentWorkflowCount; i++)
        {
            var workflowIndex = i;
            tasks.Add(Task.Run(async () => 
            {
                using var foundry = WorkflowForge.CreateFoundry($"Contention_{workflowIndex}", _config);
                
                for (int j = 0; j < OperationsPerWorkflow; j++)
                {
                    foundry.WithOperation($"ContentionOp_{j}", async (foundry) =>
                    {
                        await contentionResource.AccessResourceAsync();
                        foundry.Properties[$"accessed_{j}"] = $"Accessed_{j}";
                    });
                }
                
                await foundry.ForgeAsync();
                return $"Contention workflow {workflowIndex} completed";
            }));
        }
        
        var results = await Task.WhenAll(tasks);
        return $"Completed {results.Length} workflows under high contention, access count: {contentionResource.AccessCount}";
    }

    [Benchmark]
    public async Task<string> WorkflowChainConcurrency()
    {
        var tasks = new List<Task<string>>();
        
        for (int i = 0; i < ConcurrentWorkflowCount; i++)
        {
            var workflowIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                // Create a chain of dependent workflows
                var result1 = await RunSingleWorkflow($"Chain1_{workflowIndex}");
                var result2 = await RunSingleWorkflow($"Chain2_{workflowIndex}");
                var result3 = await RunSingleWorkflow($"Chain3_{workflowIndex}");
                
                return $"Chain {workflowIndex}: {result1} -> {result2} -> {result3}";
            }));
        }
        
        var results = await Task.WhenAll(tasks);
        return $"Completed {results.Length} workflow chains";
    }

    private async Task<string> RunSingleWorkflow(string workflowName)
    {
        using var foundry = WorkflowForge.CreateFoundry(workflowName, _config);
        
        foundry.Properties["start_time"] = DateTime.UtcNow;
        foundry.Properties["operation_count"] = 0;
        
        for (int i = 0; i < OperationsPerWorkflow; i++)
        {
            var operationIndex = i;
            foundry.WithOperation($"Op_{operationIndex}", async (foundry) =>
            {
                await Task.Delay(1); // Simulate work
                var count = (int)foundry.Properties["operation_count"]!;
                foundry.Properties["operation_count"] = count + 1;
                foundry.Properties[$"op_{operationIndex}_result"] = $"Op_{operationIndex}_Result";
            });
        }
        
        await foundry.ForgeAsync();
        
        var duration = DateTime.UtcNow - (DateTime)foundry.Properties["start_time"]!;
        return $"{workflowName} completed in {duration.TotalMilliseconds:F0}ms";
    }

    private async Task<string> RunWorkflowWithSharedResource(string workflowName, SharedBenchmarkResource sharedResource)
    {
        using var foundry = WorkflowForge.CreateFoundry(workflowName, _config);
        
        for (int i = 0; i < OperationsPerWorkflow; i++)
        {
            var operationIndex = i;
            foundry.WithOperation($"SharedOp_{operationIndex}", async (foundry) =>
            {
                await sharedResource.IncrementAsync();
                await Task.Delay(1);
                foundry.Properties[$"shared_{operationIndex}"] = $"Shared_{operationIndex}";
            });
        }
        
        await foundry.ForgeAsync();
        return $"{workflowName} completed with shared resource";
    }

    private async Task<string> RunWorkflowWithGlobalData(string workflowName, ConcurrentDictionary<string, object> globalData)
    {
        using var foundry = WorkflowForge.CreateFoundry(workflowName, _config);
        
        for (int i = 0; i < OperationsPerWorkflow; i++)
        {
            var operationIndex = i;
            foundry.WithOperation($"GlobalOp_{operationIndex}", async (foundry) =>
            {
                globalData[$"{workflowName}_op_{operationIndex}"] = DateTime.UtcNow;
                globalData[$"{workflowName}_data_{operationIndex}"] = $"Data_{operationIndex}";
                
                await Task.Delay(1);
                foundry.Properties[$"global_{operationIndex}"] = $"Global_{operationIndex}";
            });
        }
        
        await foundry.ForgeAsync();
        return $"{workflowName} completed with global data";
    }

    private async Task<T> ExecuteWithSemaphore<T>(SemaphoreSlim semaphore, Func<Task<T>> operation)
    {
        await semaphore.WaitAsync();
        try
        {
            return await operation();
        }
        finally
        {
            semaphore.Release();
        }
    }
}

/// <summary>
/// Shared resource for testing concurrent access
/// </summary>
public class SharedBenchmarkResource
{
    private readonly object _lock = new object();
    private int _counter;

    public int Counter => _counter;

    public async Task IncrementAsync()
    {
        await Task.Yield();
        lock (_lock)
        {
            _counter++;
        }
    }
}

/// <summary>
/// Resource that creates high contention for benchmarking
/// </summary>
public class HighContentionResource
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private int _accessCount;

    public int AccessCount => _accessCount;

    public async Task AccessResourceAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            // Simulate resource access with some work
            await Task.Delay(2);
            Interlocked.Increment(ref _accessCount);
        }
        finally
        {
            _semaphore.Release();
        }
    }
} 