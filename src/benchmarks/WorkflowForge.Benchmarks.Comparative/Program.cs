using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using WorkflowForge.Benchmarks.Comparative.Benchmarks;

namespace WorkflowForge.Benchmarks.Comparative;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine("  WorkflowForge Comparative Performance Benchmarks");
        Console.WriteLine("  WorkflowForge vs Workflow Core vs Elsa Workflows");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Check if running specific scenarios or all
        if (args.Length > 0)
        {
            var scenario = args[0].ToLower();
            switch (scenario)
            {
                case "scenario1":
                    Console.WriteLine("Running Scenario 1: Simple Sequential Workflow");
                    BenchmarkRunner.Run<Scenario1Benchmark>(CreateConfig());
                    break;

                case "scenario2":
                    Console.WriteLine("Running Scenario 2: Data Passing Workflow");
                    BenchmarkRunner.Run<Scenario2Benchmark>(CreateConfig());
                    break;

                case "scenario3":
                    Console.WriteLine("Running Scenario 3: Conditional Branching");
                    BenchmarkRunner.Run<Scenario3Benchmark>(CreateConfig());
                    break;

                case "scenario4":
                    Console.WriteLine("Running Scenario 4: Loop/ForEach Processing");
                    BenchmarkRunner.Run<Scenario4Benchmark>(CreateConfig());
                    break;

                case "scenario5":
                    Console.WriteLine("Running Scenario 5: Concurrent Execution");
                    BenchmarkRunner.Run<Scenario5Benchmark>(CreateConfig());
                    break;

                case "scenario6":
                    Console.WriteLine("Running Scenario 6: Error Handling");
                    BenchmarkRunner.Run<Scenario6Benchmark>(CreateConfig());
                    break;

                case "scenario7":
                    Console.WriteLine("Running Scenario 7: Creation Overhead");
                    BenchmarkRunner.Run<Scenario7Benchmark>(CreateConfig());
                    break;

                case "scenario8":
                    Console.WriteLine("Running Scenario 8: Complete Lifecycle");
                    BenchmarkRunner.Run<Scenario8Benchmark>(CreateConfig());
                    break;

                default:
                    Console.WriteLine($"Unknown scenario: {scenario}");
                    break;
            }
        }
        else
        {
            Console.WriteLine("Running ALL comparative benchmarks (8 scenarios)...");
            Console.WriteLine();
            Console.WriteLine("Available benchmarks:");
            Console.WriteLine("  1. Simple Sequential Workflow");
            Console.WriteLine("  2. Data Passing Workflow");
            Console.WriteLine("  3. Conditional Branching");
            Console.WriteLine("  4. Loop/ForEach Processing");
            Console.WriteLine("  5. Concurrent Execution");
            Console.WriteLine("  6. Error Handling");
            Console.WriteLine("  7. Creation Overhead");
            Console.WriteLine("  8. Complete Lifecycle");
            Console.WriteLine();
            Console.WriteLine("To run specific scenario: dotnet run --configuration Release -- scenario1");
            Console.WriteLine();

            // Run all benchmarks
            Console.WriteLine("Running Scenario 1...");
            BenchmarkRunner.Run<Scenario1Benchmark>(CreateConfig());
            Console.WriteLine("Running Scenario 2...");
            BenchmarkRunner.Run<Scenario2Benchmark>(CreateConfig());
            Console.WriteLine("Running Scenario 3...");
            BenchmarkRunner.Run<Scenario3Benchmark>(CreateConfig());
            Console.WriteLine("Running Scenario 4...");
            BenchmarkRunner.Run<Scenario4Benchmark>(CreateConfig());
            Console.WriteLine("Running Scenario 5...");
            BenchmarkRunner.Run<Scenario5Benchmark>(CreateConfig());
            Console.WriteLine("Running Scenario 6...");
            BenchmarkRunner.Run<Scenario6Benchmark>(CreateConfig());
            Console.WriteLine("Running Scenario 7...");
            BenchmarkRunner.Run<Scenario7Benchmark>(CreateConfig());
            Console.WriteLine("Running Scenario 8...");
            BenchmarkRunner.Run<Scenario8Benchmark>(CreateConfig());

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("  ALL BENCHMARKS COMPLETE!");
            Console.WriteLine("  Results saved to: BenchmarkDotNet.Artifacts/results/");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        }
    }

    private static IConfig CreateConfig()
    {
        // Use default configuration from benchmark attributes (5 warmup, 15 iterations)
        return DefaultConfig.Instance
            .WithOption(ConfigOptions.DisableOptimizationsValidator, true)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddColumn(StatisticColumn.Median)
            .AddColumn(StatisticColumn.P95)
            .AddColumn(StatisticColumn.StdDev)
            .AddExporter(MarkdownExporter.GitHub)
            .AddExporter(HtmlExporter.Default)
            .AddExporter(BenchmarkDotNet.Exporters.Csv.CsvExporter.Default);
    }
}