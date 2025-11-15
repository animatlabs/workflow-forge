using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System.Collections.Concurrent;
using System.Text;
using WorkflowForge.Extensions;

namespace WorkflowForge.Benchmarks;

/// <summary>
/// Benchmarks memory allocation patterns and GC impact:
/// - Object allocation rates
/// - Memory pressure scenarios
/// - Garbage collection efficiency
/// - Memory-optimized configurations
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, iterationCount: 25)]
[MarkdownExporter]
[HtmlExporter]
public class MemoryAllocationBenchmark
{
    [Params(10, 50, 100, 500)]
    public int AllocationCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Benchmarks use defaults - no custom configuration needed
    }

    [Benchmark(Baseline = true)]
    public async Task<string> MinimalAllocationWorkflow()
    {
        using var foundry = WorkflowForge.CreateFoundry("MinimalAllocation");

        foundry.WithOperation("MinimalOp", async (foundry) =>
        {
            // Minimal allocation - just return a string
            await Task.Yield();
            foundry.Properties["result"] = "minimal";
        });

        await foundry.ForgeAsync();
        return "Completed minimal allocation";
    }

    [Benchmark]
    public async Task<string> SmallObjectAllocation()
    {
        using var foundry = WorkflowForge.CreateFoundry("SmallObjects");

        for (int i = 0; i < AllocationCount; i++)
        {
            var index = i;
            foundry.WithOperation($"SmallOp_{index}", async (foundry) =>
            {
                // Allocate small objects
                var data = new { Id = index, Name = $"Item_{index}", Timestamp = DateTime.UtcNow };
                foundry.Properties[$"small_{index}"] = data;
                await Task.Yield();
            });
        }

        await foundry.ForgeAsync();
        return $"Completed {AllocationCount} small object allocations";
    }

    [Benchmark]
    public async Task<string> LargeObjectAllocation()
    {
        using var foundry = WorkflowForge.CreateFoundry("LargeObjects");

        for (int i = 0; i < Math.Min(AllocationCount, 50); i++) // Limit for large objects
        {
            var index = i;
            foundry.WithOperation($"LargeOp_{index}", async (foundry) =>
            {
                // Allocate large objects (85KB+ to go to LOH)
                var largeData = new byte[100_000];
                Array.Fill(largeData, (byte)(index % 256));
                foundry.Properties[$"large_{index}"] = largeData;
                await Task.Yield();
            });
        }

        await foundry.ForgeAsync();
        return $"Completed {Math.Min(AllocationCount, 50)} large object allocations";
    }

    [Benchmark]
    public async Task<string> StringConcatenationAllocation()
    {
        using var foundry = WorkflowForge.CreateFoundry("StringConcat");

        foundry.Properties["result_string"] = "";

        for (int i = 0; i < AllocationCount; i++)
        {
            var index = i;
            foundry.WithOperation($"StringOp_{index}", async (foundry) =>
            {
                // Create string allocation pressure
                var current = (string)foundry.Properties["result_string"]!;
                var newString = current + $"Item_{index}_";
                foundry.Properties["result_string"] = newString;
                await Task.Yield();
            });
        }

        await foundry.ForgeAsync();
        var finalString = (string)foundry.Properties["result_string"]!;
        return $"Completed string concatenation, final length: {finalString.Length}";
    }

    [Benchmark]
    public async Task<string> StringBuilderOptimization()
    {
        using var foundry = WorkflowForge.CreateFoundry("StringBuilder");

        var stringBuilder = new StringBuilder(AllocationCount * 20); // Pre-size for efficiency
        foundry.Properties["string_builder"] = stringBuilder;

        for (int i = 0; i < AllocationCount; i++)
        {
            var index = i;
            foundry.WithOperation($"SBOp_{index}", async (foundry) =>
            {
                var sb = (StringBuilder)foundry.Properties["string_builder"]!;
                sb.Append($"Item_{index}_");
                await Task.Yield();
            });
        }

        await foundry.ForgeAsync();
        var result = stringBuilder.ToString();
        return $"Completed StringBuilder optimization, final length: {result.Length}";
    }

    [Benchmark]
    public async Task<string> CollectionAllocation()
    {
        using var foundry = WorkflowForge.CreateFoundry("Collections");

        var collections = new List<List<string>>();
        foundry.Properties["collections"] = collections;

        for (int i = 0; i < AllocationCount; i++)
        {
            var index = i;
            foundry.WithOperation($"CollOp_{index}", async (foundry) =>
            {
                var colls = (List<List<string>>)foundry.Properties["collections"]!;
                var newList = new List<string> { $"Item_{index}", $"Value_{index}", $"Data_{index}" };
                colls.Add(newList);
                await Task.Yield();
            });
        }

        await foundry.ForgeAsync();
        return $"Completed {collections.Count} collection allocations with {collections.Sum(c => c.Count)} total items";
    }

    [Benchmark]
    public async Task<string> ObjectPoolingSimulation()
    {
        using var foundry = WorkflowForge.CreateFoundry("ObjectPooling");

        var objectPool = new SimpleObjectPool<BenchmarkWorkObject>(AllocationCount);
        foundry.Properties["object_pool"] = objectPool;

        for (int i = 0; i < AllocationCount; i++)
        {
            var index = i;
            foundry.WithOperation($"PoolOp_{index}", async (foundry) =>
            {
                var pool = (SimpleObjectPool<BenchmarkWorkObject>)foundry.Properties["object_pool"]!;
                var obj = pool.Get();
                try
                {
                    obj.Initialize(index, $"Pooled_{index}");
                    await Task.Yield();
                    foundry.Properties[$"pool_result_{index}"] = obj.ProcessData();
                }
                finally
                {
                    pool.Return(obj);
                }
            });
        }

        await foundry.ForgeAsync();
        return $"Completed {AllocationCount} operations with object pooling";
    }

    [Benchmark]
    public async Task<string> MemoryPressureScenario()
    {
        using var foundry = WorkflowForge.CreateFoundry("MemoryPressure");

        var memoryIntensiveData = new List<byte[]>();
        foundry.Properties["memory_data"] = memoryIntensiveData;

        for (int i = 0; i < Math.Min(AllocationCount, 100); i++) // Limit to prevent OOM
        {
            var index = i;
            foundry.WithOperation($"MemOp_{index}", async (foundry) =>
            {
                var data = (List<byte[]>)foundry.Properties["memory_data"]!;

                // Allocate various sizes to create memory pressure
                var size = (index % 5 + 1) * 10_000; // 10KB to 50KB
                var allocation = new byte[size];
                Array.Fill(allocation, (byte)(index % 256));
                data.Add(allocation);

                // Occasionally force GC to test pressure handling
                if (index % 20 == 0)
                {
                    GC.Collect(0, GCCollectionMode.Optimized);
                }

                await Task.Yield();
            });
        }

        await foundry.ForgeAsync();
        var totalMemory = memoryIntensiveData.Sum(data => data.Length);
        return $"Completed memory pressure scenario, total allocated: {totalMemory:N0} bytes";
    }

    [Benchmark]
    public async Task<string> DisposableResourceManagement()
    {
        using var foundry = WorkflowForge.CreateFoundry("DisposableResources");

        var disposedCount = 0;
        foundry.Properties["disposed_count"] = disposedCount;

        for (int i = 0; i < AllocationCount; i++)
        {
            var index = i;
            foundry.WithOperation($"DispOp_{index}", async (foundry) =>
            {
                using var disposableResource = new DisposableBenchmarkResource(() =>
                {
                    var count = (int)foundry.Properties["disposed_count"]!;
                    foundry.Properties["disposed_count"] = count + 1;
                });

                await disposableResource.DoWorkAsync(index);
                await Task.Yield();
            });
        }

        await foundry.ForgeAsync();
        var finalDisposedCount = (int)foundry.Properties["disposed_count"]!;
        return $"Completed {AllocationCount} operations, disposed {finalDisposedCount} resources";
    }

    [Benchmark]
    public async Task<string> ArrayReuseOptimization()
    {
        using var foundry = WorkflowForge.CreateFoundry("ArrayReuse");

        var reuseableArray = new int[1000]; // Reuse same array
        foundry.Properties["reuseable_array"] = reuseableArray;

        for (int i = 0; i < AllocationCount; i++)
        {
            var index = i;
            foundry.WithOperation($"ReuseOp_{index}", async (foundry) =>
            {
                var array = (int[])foundry.Properties["reuseable_array"]!;

                // Reuse array instead of allocating new one
                Array.Fill(array, index, 0, Math.Min(array.Length, index + 1));

                await Task.Yield();
                foundry.Properties[$"array_sum_{index}"] = array.Sum();
            });
        }

        await foundry.ForgeAsync();
        return $"Completed {AllocationCount} operations with array reuse";
    }
}

/// <summary>
/// Simple object pool for benchmarking
/// </summary>
public class SimpleObjectPool<T> where T : class, new()
{
    private readonly ConcurrentQueue<T> _objects = new();
    private readonly Func<T>? _objectGenerator;

    public SimpleObjectPool(int initialCount = 0, Func<T>? objectGenerator = null)
    {
        _objectGenerator = objectGenerator;
        for (int i = 0; i < initialCount; i++)
        {
            _objects.Enqueue(CreateObject());
        }
    }

    public T Get()
    {
        return _objects.TryDequeue(out var item) ? item : CreateObject();
    }

    public void Return(T item)
    {
        _objects.Enqueue(item);
    }

    private T CreateObject()
    {
        return _objectGenerator?.Invoke() ?? new T();
    }
}

/// <summary>
/// Benchmark work object for pooling
/// </summary>
public class BenchmarkWorkObject
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime ProcessedAt { get; private set; }

    public void Initialize(int id, string name)
    {
        Id = id;
        Name = name;
        ProcessedAt = DateTime.UtcNow;
    }

    public string ProcessData()
    {
        return $"Processed {Name} with ID {Id} at {ProcessedAt:HH:mm:ss.fff}";
    }
}

/// <summary>
/// Disposable resource for testing resource management
/// </summary>
public class DisposableBenchmarkResource : IDisposable
{
    private readonly Action _onDispose;
    private bool _disposed;

    public DisposableBenchmarkResource(Action onDispose)
    {
        _onDispose = onDispose;
    }

    public async Task DoWorkAsync(int workId)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DisposableBenchmarkResource));

        await Task.Delay(1);
        // Simulate work with the resource
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _onDispose?.Invoke();
            _disposed = true;
        }
    }
}