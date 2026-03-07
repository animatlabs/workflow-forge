using System.Runtime.InteropServices;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
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

            BenchmarkRunner.Run<OperationPerformanceBenchmark>(CreateConfig());
            BenchmarkRunner.Run<WorkflowThroughputBenchmark>(CreateConfig());
            BenchmarkRunner.Run<MemoryAllocationBenchmark>(CreateConfig());
            BenchmarkRunner.Run<ConcurrencyBenchmark>(CreateConfig());

            Console.WriteLine();
            Console.WriteLine("================================================================");
            Console.WriteLine("  ALL INTERNAL BENCHMARKS COMPLETE!");
            Console.WriteLine("  Results saved to: BenchmarkDotNet.Artifacts/results/");
            Console.WriteLine("================================================================");
        }
        else
        {
            var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
            switcher.Run(args, CreateConfig());
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
            b.Setup();
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
            try
            {
                action();
                Console.WriteLine($"  [PASS] {name}");
                return true;
            }
            catch (Exception ex)
            {
                // Expected benchmark failure: report and continue with other validations.
                Console.WriteLine($"  [FAIL] {name}: {ex.GetType().FullName}: {ex.Message}");
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            // Non-critical exception that escaped the inner handler; treat as a benchmark failure.
            Console.WriteLine($"  [FAIL] {name}: {ex.GetType().FullName}: {ex.Message}");
            Console.WriteLine(ex.ToString());
            return false;
        }
    }

    private static bool IsCriticalException(Exception ex)
    {
        return ex is OutOfMemoryException
            or ThreadAbortException
            or StackOverflowException
            or ThreadInterruptedException
            or AccessViolationException;
    }

    private static IConfig CreateConfig()
    {
        return DefaultConfig.Instance
            .WithOption(ConfigOptions.DisableOptimizationsValidator, true)
            .AddJob(Job.Default.WithRuntime(ClrRuntime.Net48)
                .WithStrategy(RunStrategy.Monitoring)
                .WithIterationCount(50)
                .WithInvocationCount(1).WithUnrollFactor(1))
            .AddJob(Job.Default.WithRuntime(CoreRuntime.Core80)
                .WithStrategy(RunStrategy.Monitoring)
                .WithIterationCount(50)
                .WithInvocationCount(1).WithUnrollFactor(1))
            .AddJob(Job.Default.WithRuntime(CoreRuntime.Core10_0)
                .WithStrategy(RunStrategy.Monitoring)
                .WithIterationCount(50)
                .WithInvocationCount(1).WithUnrollFactor(1))
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddColumn(StatisticColumn.Median)
            .AddColumn(StatisticColumn.P95)
            .AddColumn(StatisticColumn.StdDev)
            .AddExporter(MarkdownExporter.GitHub)
            .AddExporter(HtmlExporter.Default)
            .AddExporter(BenchmarkDotNet.Exporters.Csv.CsvExporter.Default);
    }
}
