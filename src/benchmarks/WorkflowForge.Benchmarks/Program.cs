using BenchmarkDotNet.Running;

namespace WorkflowForge.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("================================================================");
        Console.WriteLine("  WorkflowForge Internal Performance Benchmarks");
        Console.WriteLine("  Comprehensive performance testing of WorkflowForge framework");
        Console.WriteLine("================================================================");
        Console.WriteLine();

        if (args.Length == 0)
        {
            Console.WriteLine("Running ALL internal benchmarks...");
            Console.WriteLine();
            Console.WriteLine("Available benchmarks:");
            Console.WriteLine("  1. Operation Performance Benchmark");
            Console.WriteLine("  2. Workflow Throughput Benchmark");
            Console.WriteLine("  3. Memory Allocation Benchmark");
            Console.WriteLine("  4. Concurrency Benchmark");
            Console.WriteLine("  5. Configuration Profiles Benchmark");
            Console.WriteLine();
            Console.WriteLine("Running all benchmarks automatically...");
            Console.WriteLine();

            // Run all benchmarks
            BenchmarkRunner.Run<OperationPerformanceBenchmark>();
            BenchmarkRunner.Run<WorkflowThroughputBenchmark>();
            BenchmarkRunner.Run<MemoryAllocationBenchmark>();
            BenchmarkRunner.Run<ConcurrencyBenchmark>();

            Console.WriteLine();
            Console.WriteLine("================================================================");
            Console.WriteLine("  ALL INTERNAL BENCHMARKS COMPLETE!");
            Console.WriteLine("  Results saved to: BenchmarkDotNet.Artifacts/results/");
            Console.WriteLine("================================================================");
        }
        else
        {
            // Use BenchmarkSwitcher for custom arguments
            var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
            switcher.Run(args);
        }
    }
}