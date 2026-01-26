using BenchmarkDotNet.Attributes;
using WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

/// <summary>
/// Scenario 1: Simple Sequential Workflow Benchmark
/// Compares WorkflowForge vs Workflow Core vs Elsa for basic sequential execution
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 5, iterationCount: 25)]
[MarkdownExporter]
[HtmlExporter]
public class Scenario1Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;

    [Params(1, 5, 10, 25, 50)]
    public int OperationCount { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters { OperationCount = OperationCount };

        // Setup all three framework implementations FRESH for each iteration
        _workflowForgeScenario = new Scenario1_SimpleSequential_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();

        _workflowCoreScenario = new Scenario1_SimpleSequential_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();

        _elsaScenario = new Scenario1_SimpleSequential_Elsa(parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _workflowForgeScenario.CleanupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario.CleanupAsync().GetAwaiter().GetResult();
        _elsaScenario.CleanupAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true, Description = "WorkflowForge - Simple Sequential")]
    public async Task<ScenarioResult> WorkflowForge_SimpleSequential()
    {
        return await _workflowForgeScenario.ExecuteAsync();
    }

    [Benchmark(Description = "WorkflowCore - Simple Sequential")]
    public async Task<ScenarioResult> WorkflowCore_SimpleSequential()
    {
        return await _workflowCoreScenario.ExecuteAsync();
    }

    [Benchmark(Description = "Elsa - Simple Sequential")]
    public async Task<ScenarioResult> Elsa_SimpleSequential()
    {
        return await _elsaScenario.ExecuteAsync();
    }
}