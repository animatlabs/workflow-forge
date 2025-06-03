using BenchmarkDotNet.Running;
using WorkflowForge.Benchmarks;

namespace WorkflowForge.Benchmarks;

/// <summary>
/// WorkflowForge Benchmarks Runner
/// 
/// Benchmarks various WorkflowForge scenarios including:
/// - Configuration profiles performance
/// - Workflow operation throughput
/// - Memory allocation patterns
/// - Concurrency scenarios
/// - Extension overhead analysis
/// 
/// Usage:
///   dotnet run -c Release                    # Run all benchmarks
///   dotnet run -c Release --filter "*Config*" # Run configuration benchmarks only
///   dotnet run -c Release --filter "*Throughput*" # Run throughput benchmarks only
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("WorkflowForge Performance Benchmarks");
        Console.WriteLine("=====================================");
        Console.WriteLine();
        Console.WriteLine("Running comprehensive performance analysis...");
        Console.WriteLine("This will take several minutes to complete.");
        Console.WriteLine();

        var switcher = new BenchmarkSwitcher(new[]
        {
            typeof(ConfigurationProfilesBenchmark),
            typeof(WorkflowThroughputBenchmark),
            typeof(OperationPerformanceBenchmark),
            typeof(ConcurrencyBenchmark),
            typeof(MemoryAllocationBenchmark)
        });

        switcher.Run(args);
    }
} 