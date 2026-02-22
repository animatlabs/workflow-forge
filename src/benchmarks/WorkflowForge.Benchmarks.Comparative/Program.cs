using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using System.Runtime.InteropServices;
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

        if (args.Length > 0 && args[0].Equals("--validate", StringComparison.OrdinalIgnoreCase))
        {
            RunValidationMode();
            return;
        }

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

                case "scenario9":
                    Console.WriteLine("Running Scenario 9: State Machine");
                    BenchmarkRunner.Run<Scenario9Benchmark>(CreateConfig());
                    break;

                case "scenario10":
                    Console.WriteLine("Running Scenario 10: Long Running");
                    BenchmarkRunner.Run<Scenario10Benchmark>(CreateConfig());
                    break;

                case "scenario11":
                    Console.WriteLine("Running Scenario 11: Parallel Execution");
                    BenchmarkRunner.Run<Scenario11Benchmark>(CreateConfig());
                    break;

                case "scenario12":
                    Console.WriteLine("Running Scenario 12: Event-Driven");
                    BenchmarkRunner.Run<Scenario12Benchmark>(CreateConfig());
                    break;

                default:
                    Console.WriteLine($"Unknown scenario: {scenario}");
                    break;
            }
        }
        else
        {
            Console.WriteLine("Running ALL comparative benchmarks (12 scenarios)...");
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
            Console.WriteLine("  9. State Machine");
            Console.WriteLine("  10. Long Running");
            Console.WriteLine("  11. Parallel Execution");
            Console.WriteLine("  12. Event-Driven");
            Console.WriteLine();
            Console.WriteLine("To run specific scenario: dotnet run --configuration Release -- scenario1");
            Console.WriteLine("To validate all scenarios: dotnet run --configuration Release -- --validate");
            Console.WriteLine();

            BenchmarkRunner.Run<Scenario1Benchmark>(CreateConfig());
            BenchmarkRunner.Run<Scenario2Benchmark>(CreateConfig());
            BenchmarkRunner.Run<Scenario3Benchmark>(CreateConfig());
            BenchmarkRunner.Run<Scenario4Benchmark>(CreateConfig());
            BenchmarkRunner.Run<Scenario5Benchmark>(CreateConfig());
            BenchmarkRunner.Run<Scenario6Benchmark>(CreateConfig());
            BenchmarkRunner.Run<Scenario7Benchmark>(CreateConfig());
            BenchmarkRunner.Run<Scenario8Benchmark>(CreateConfig());
            BenchmarkRunner.Run<Scenario9Benchmark>(CreateConfig());
            BenchmarkRunner.Run<Scenario10Benchmark>(CreateConfig());
            BenchmarkRunner.Run<Scenario11Benchmark>(CreateConfig());
            BenchmarkRunner.Run<Scenario12Benchmark>(CreateConfig());

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("  ALL BENCHMARKS COMPLETE!");
            Console.WriteLine("  Results saved to: BenchmarkDotNet.Artifacts/results/");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        }
    }

    private static void RunValidationMode()
    {
        Console.WriteLine("VALIDATION MODE: Running each scenario once to verify correctness...");
        Console.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"Elsa supported: {ElsaScenarioFactory.IsSupported}");
        Console.WriteLine();

        var allPassed = true;

        // Scenario 1: Simple Sequential
        allPassed &= ValidateScenario("Scenario1 - WorkflowForge", () =>
        {
            var b = new Scenario1Benchmark { OperationCount = 3 };
            b.Setup();
            var r = b.WorkflowForge_SimpleSequential().GetAwaiter().GetResult();
            b.Cleanup();
            if (!r.Success) throw new Exception("Scenario did not succeed");
        });
        allPassed &= ValidateScenario("Scenario1 - WorkflowCore", () =>
        {
            var b = new Scenario1Benchmark { OperationCount = 3 };
            b.Setup();
            var r = b.WorkflowCore_SimpleSequential().GetAwaiter().GetResult();
            b.Cleanup();
            if (!r.Success) throw new Exception("Scenario did not succeed");
        });
        allPassed &= ValidateScenario("Scenario1 - Elsa", () =>
        {
            var b = new Scenario1Benchmark { OperationCount = 3 };
            b.Setup();
            var r = b.Elsa_SimpleSequential().GetAwaiter().GetResult();
            b.Cleanup();
        });

        // Scenario 2: Data Passing
        allPassed &= ValidateScenario("Scenario2 - WorkflowForge", () =>
        {
            var b = new Scenario2Benchmark { OperationCount = 3 };
            b.Setup();
            var r = b.WorkflowForge_DataPassing().GetAwaiter().GetResult();
            b.Cleanup();
            if (!r.Success) throw new Exception("Scenario did not succeed");
        });
        allPassed &= ValidateScenario("Scenario2 - WorkflowCore", () =>
        {
            var b = new Scenario2Benchmark { OperationCount = 3 };
            b.Setup();
            var r = b.WorkflowCore_DataPassing().GetAwaiter().GetResult();
            b.Cleanup();
            if (!r.Success) throw new Exception("Scenario did not succeed");
        });
        allPassed &= ValidateScenario("Scenario2 - Elsa", () =>
        {
            var b = new Scenario2Benchmark { OperationCount = 3 };
            b.Setup();
            var r = b.Elsa_DataPassing().GetAwaiter().GetResult();
            b.Cleanup();
        });

        // Scenario 3: Conditional Branching
        allPassed &= ValidateScenario("Scenario3 - WorkflowForge", () =>
        {
            var b = new Scenario3Benchmark { OperationCount = 10 };
            b.Setup();
            var r = b.WorkflowForge_ConditionalBranching().GetAwaiter().GetResult();
            b.Cleanup();
            if (!r.Success) throw new Exception("Scenario did not succeed");
        });
        allPassed &= ValidateScenario("Scenario3 - WorkflowCore", () =>
        {
            var b = new Scenario3Benchmark { OperationCount = 10 };
            b.Setup();
            var r = b.WorkflowCore_ConditionalBranching().GetAwaiter().GetResult();
            b.Cleanup();
            if (!r.Success) throw new Exception("Scenario did not succeed");
        });
        allPassed &= ValidateScenario("Scenario3 - Elsa", () =>
        {
            var b = new Scenario3Benchmark { OperationCount = 10 };
            b.Setup();
            var r = b.Elsa_ConditionalBranching().GetAwaiter().GetResult();
            b.Cleanup();
        });

        // Scenario 4: Loop Processing
        allPassed &= ValidateScenario("Scenario4 - WorkflowForge", () =>
        {
            var b = new Scenario4Benchmark { ItemCount = 10 };
            b.Setup();
            var r = b.WorkflowForge_LoopProcessing().GetAwaiter().GetResult();
            b.Cleanup();
            if (!r.Success) throw new Exception("Scenario did not succeed");
        });
        allPassed &= ValidateScenario("Scenario4 - WorkflowCore", () =>
        {
            var b = new Scenario4Benchmark { ItemCount = 10 };
            b.Setup();
            var r = b.WorkflowCore_LoopProcessing().GetAwaiter().GetResult();
            b.Cleanup();
            if (!r.Success) throw new Exception("Scenario did not succeed");
        });
        allPassed &= ValidateScenario("Scenario4 - Elsa", () =>
        {
            var b = new Scenario4Benchmark { ItemCount = 10 };
            b.Setup();
            var r = b.Elsa_LoopProcessing().GetAwaiter().GetResult();
            b.Cleanup();
        });

        // Scenario 5: Concurrent Execution
        allPassed &= ValidateScenario("Scenario5 - WorkflowForge", () =>
        {
            var b = new Scenario5Benchmark { ConcurrencyLevel = 2 };
            b.Setup();
            var r = b.WorkflowForge_ConcurrentExecution().GetAwaiter().GetResult();
            b.Cleanup();
            if (!r.Success) throw new Exception("Scenario did not succeed");
        });
        allPassed &= ValidateScenario("Scenario5 - WorkflowCore", () =>
        {
            var b = new Scenario5Benchmark { ConcurrencyLevel = 2 };
            b.Setup();
            var r = b.WorkflowCore_ConcurrentExecution().GetAwaiter().GetResult();
            b.Cleanup();
        });
        allPassed &= ValidateScenario("Scenario5 - Elsa", () =>
        {
            var b = new Scenario5Benchmark { ConcurrencyLevel = 2 };
            b.Setup();
            var r = b.Elsa_ConcurrentExecution().GetAwaiter().GetResult();
            b.Cleanup();
        });

        // Scenario 6: Error Handling
        allPassed &= ValidateScenario("Scenario6 - WorkflowForge", () =>
        {
            var b = new Scenario6Benchmark();
            b.Setup();
            var r = b.WorkflowForge_ErrorHandling().GetAwaiter().GetResult();
            b.Cleanup();
        });
        allPassed &= ValidateScenario("Scenario6 - WorkflowCore", () =>
        {
            var b = new Scenario6Benchmark();
            b.Setup();
            var r = b.WorkflowCore_ErrorHandling().GetAwaiter().GetResult();
            b.Cleanup();
        });
        allPassed &= ValidateScenario("Scenario6 - Elsa", () =>
        {
            var b = new Scenario6Benchmark();
            b.Setup();
            var r = b.Elsa_ErrorHandling().GetAwaiter().GetResult();
            b.Cleanup();
        });

        // Scenario 7: Creation Overhead
        allPassed &= ValidateScenario("Scenario7 - WorkflowForge", () =>
        {
            var b = new Scenario7Benchmark();
            b.Setup();
            var r = b.WorkflowForge_CreationOverhead().GetAwaiter().GetResult();
            b.Cleanup();
        });
        allPassed &= ValidateScenario("Scenario7 - WorkflowCore", () =>
        {
            var b = new Scenario7Benchmark();
            b.Setup();
            var r = b.WorkflowCore_CreationOverhead().GetAwaiter().GetResult();
            b.Cleanup();
        });
        allPassed &= ValidateScenario("Scenario7 - Elsa", () =>
        {
            var b = new Scenario7Benchmark();
            b.Setup();
            var r = b.Elsa_CreationOverhead().GetAwaiter().GetResult();
            b.Cleanup();
        });

        // Scenario 8: Complete Lifecycle (no WorkflowCore)
        allPassed &= ValidateScenario("Scenario8 - WorkflowForge", () =>
        {
            var b = new Scenario8Benchmark();
            b.Setup();
            var r = b.WorkflowForge_CompleteLifecycle().GetAwaiter().GetResult();
            b.Cleanup();
        });
        allPassed &= ValidateScenario("Scenario8 - Elsa", () =>
        {
            var b = new Scenario8Benchmark();
            b.Setup();
            var r = b.Elsa_CompleteLifecycle().GetAwaiter().GetResult();
            b.Cleanup();
        });

        // Scenario 9: State Machine
        allPassed &= ValidateScenario("Scenario9 - WorkflowForge", () =>
        {
            var b = new Scenario9Benchmark { TransitionCount = 5 };
            b.Setup();
            var r = b.WorkflowForge_StateMachine().GetAwaiter().GetResult();
            b.Cleanup();
        });
        allPassed &= ValidateScenario("Scenario9 - WorkflowCore", () =>
        {
            var b = new Scenario9Benchmark { TransitionCount = 5 };
            b.Setup();
            var r = b.WorkflowCore_StateMachine().GetAwaiter().GetResult();
            b.Cleanup();
        });
        allPassed &= ValidateScenario("Scenario9 - Elsa", () =>
        {
            var b = new Scenario9Benchmark { TransitionCount = 5 };
            b.Setup();
            var r = b.Elsa_StateMachine().GetAwaiter().GetResult();
            b.Cleanup();
        });

        // Scenario 10: Long Running
        allPassed &= ValidateScenario("Scenario10 - WorkflowForge", () =>
        {
            var b = new Scenario10Benchmark { OperationCount = 3, DelayMilliseconds = 1 };
            b.Setup();
            var r = b.WorkflowForge_LongRunning().GetAwaiter().GetResult();
            b.Cleanup();
        });
        allPassed &= ValidateScenario("Scenario10 - WorkflowCore", () =>
        {
            var b = new Scenario10Benchmark { OperationCount = 3, DelayMilliseconds = 1 };
            b.Setup();
            var r = b.WorkflowCore_LongRunning().GetAwaiter().GetResult();
            b.Cleanup();
        });
        allPassed &= ValidateScenario("Scenario10 - Elsa", () =>
        {
            var b = new Scenario10Benchmark { OperationCount = 3, DelayMilliseconds = 1 };
            b.Setup();
            var r = b.Elsa_LongRunning().GetAwaiter().GetResult();
            b.Cleanup();
        });

        // Scenario 11: Parallel Execution
        allPassed &= ValidateScenario("Scenario11 - WorkflowForge", () =>
        {
            var b = new Scenario11Benchmark { OperationCount = 4, ConcurrencyLevel = 2 };
            b.Setup();
            var r = b.WorkflowForge_ParallelExecution().GetAwaiter().GetResult();
            b.Cleanup();
        });
        allPassed &= ValidateScenario("Scenario11 - WorkflowCore", () =>
        {
            var b = new Scenario11Benchmark { OperationCount = 4, ConcurrencyLevel = 2 };
            b.Setup();
            var r = b.WorkflowCore_ParallelExecution().GetAwaiter().GetResult();
            b.Cleanup();
        });
        allPassed &= ValidateScenario("Scenario11 - Elsa", () =>
        {
            var b = new Scenario11Benchmark { OperationCount = 4, ConcurrencyLevel = 2 };
            b.Setup();
            var r = b.Elsa_ParallelExecution().GetAwaiter().GetResult();
            b.Cleanup();
        });

        // Scenario 12: Event-Driven
        allPassed &= ValidateScenario("Scenario12 - WorkflowForge", () =>
        {
            var b = new Scenario12Benchmark { DelayMilliseconds = 1 };
            b.Setup();
            var r = b.WorkflowForge_EventDriven().GetAwaiter().GetResult();
            b.Cleanup();
        });
        allPassed &= ValidateScenario("Scenario12 - WorkflowCore", () =>
        {
            var b = new Scenario12Benchmark { DelayMilliseconds = 1 };
            b.Setup();
            var r = b.WorkflowCore_EventDriven().GetAwaiter().GetResult();
            b.Cleanup();
        });
        allPassed &= ValidateScenario("Scenario12 - Elsa", () =>
        {
            var b = new Scenario12Benchmark { DelayMilliseconds = 1 };
            b.Setup();
            var r = b.Elsa_EventDriven().GetAwaiter().GetResult();
            b.Cleanup();
        });

        Console.WriteLine();
        Console.WriteLine(allPassed
            ? "═══ ALL VALIDATIONS PASSED ═══"
            : "═══ SOME VALIDATIONS FAILED ═══");
        Environment.Exit(allPassed ? 0 : 1);
    }

    private static bool ValidateScenario(string name, Action action)
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

    private static IConfig CreateConfig()
    {
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