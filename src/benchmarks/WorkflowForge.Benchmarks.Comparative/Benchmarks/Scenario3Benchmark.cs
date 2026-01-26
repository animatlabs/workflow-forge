using BenchmarkDotNet.Attributes;
using WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 5, iterationCount: 50)]
[MarkdownExporter]
[HtmlExporter]
public class Scenario3Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;

    [Params(10, 25, 50)]
    public int OperationCount { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters { OperationCount = OperationCount };
        _workflowForgeScenario = new Scenario3_ConditionalBranching_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario = new Scenario3_ConditionalBranching_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = new Scenario3_ConditionalBranching_Elsa(parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _workflowForgeScenario.CleanupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario.CleanupAsync().GetAwaiter().GetResult();
        _elsaScenario.CleanupAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true, Description = "WorkflowForge - Conditional Branching")]
    public async Task<ScenarioResult> WorkflowForge_ConditionalBranching() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Conditional Branching")]
    public async Task<ScenarioResult> WorkflowCore_ConditionalBranching() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Conditional Branching")]
    public async Task<ScenarioResult> Elsa_ConditionalBranching() => await _elsaScenario.ExecuteAsync();
}