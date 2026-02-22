using BenchmarkDotNet.Running;
using System.Runtime.InteropServices;

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

        if (args.Length > 0 && args[0].Equals("--validate", StringComparison.OrdinalIgnoreCase))
        {
            RunValidationMode();
            return;
        }

        if (args.Length == 0)
        {
            Console.WriteLine("Running ALL internal benchmarks...");
            Console.WriteLine();
            Console.WriteLine("Available benchmarks:");
            Console.WriteLine("  1. Operation Performance Benchmark");
            Console.WriteLine("  2. Workflow Throughput Benchmark");
            Console.WriteLine("  3. Memory Allocation Benchmark");
            Console.WriteLine("  4. Concurrency Benchmark");
            Console.WriteLine();
            Console.WriteLine("To validate all benchmarks: dotnet run --configuration Release -- --validate");
            Console.WriteLine();

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
            var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
            switcher.Run(args);
        }
    }

    private static void RunValidationMode()
    {
        Console.WriteLine("VALIDATION MODE: Running each benchmark once to verify correctness...");
        Console.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine();

        var allPassed = true;

        allPassed &= ValidateBenchmark("OperationPerformance", () =>
        {
            var b = new OperationPerformanceBenchmark();
            b.Setup();
            b.DelegateOperationExecution().GetAwaiter().GetResult();
            b.CustomOperationExecution().GetAwaiter().GetResult();
            b.ConditionalOperationTrue().GetAwaiter().GetResult();
            b.ChainedOperationsExecution().GetAwaiter().GetResult();
            b.Cleanup();
        });

        allPassed &= ValidateBenchmark("WorkflowThroughput", () =>
        {
            var b = new WorkflowThroughputBenchmark { OperationCount = 3 };
            b.Setup();
            b.SequentialDelegateOperations().GetAwaiter().GetResult();
            b.SequentialCustomOperations().GetAwaiter().GetResult();
            b.DataPassingWorkflow().GetAwaiter().GetResult();
            b.MemoryIntensiveWorkflow().GetAwaiter().GetResult();
        });

        allPassed &= ValidateBenchmark("MemoryAllocation", () =>
        {
            var b = new MemoryAllocationBenchmark { AllocationCount = 10 };
            b.Setup();
            b.MinimalAllocationWorkflow().GetAwaiter().GetResult();
            b.SmallObjectAllocation().GetAwaiter().GetResult();
            b.LargeObjectAllocation().GetAwaiter().GetResult();
            b.ArrayReuseOptimization().GetAwaiter().GetResult();
        });

        allPassed &= ValidateBenchmark("Concurrency", () =>
        {
            var b = new ConcurrencyBenchmark { ConcurrentWorkflowCount = 2, OperationsPerWorkflow = 3 };
            ConcurrencyBenchmark.Setup();
            b.SequentialWorkflows().GetAwaiter().GetResult();
            b.ConcurrentWorkflows().GetAwaiter().GetResult();
            b.ParallelWorkflows().GetAwaiter().GetResult();
            b.TaskBasedConcurrency().GetAwaiter().GetResult();
        });

        Console.WriteLine();
        Console.WriteLine(allPassed
            ? "═══ ALL VALIDATIONS PASSED ═══"
            : "═══ SOME VALIDATIONS FAILED ═══");
        Environment.Exit(allPassed ? 0 : 1);
    }

    private static bool ValidateBenchmark(string name, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  [PASS] {name}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] {name}: {ex.Message}");
            return false;
        }
    }
}