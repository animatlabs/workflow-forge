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
public class Scenario9Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;

    [Params(5, 10, 25)]
    public int TransitionCount { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters { OperationCount = TransitionCount };
        _workflowForgeScenario = new Scenario9_StateMachine_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario = new Scenario9_StateMachine_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = new Scenario9_StateMachine_Elsa(parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _workflowForgeScenario.CleanupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario.CleanupAsync().GetAwaiter().GetResult();
        _elsaScenario.CleanupAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true, Description = "WorkflowForge - State Machine")]
    public async Task<ScenarioResult> WorkflowForge_StateMachine() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - State Machine")]
    public async Task<ScenarioResult> WorkflowCore_StateMachine() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - State Machine")]
    public async Task<ScenarioResult> Elsa_StateMachine() => await _elsaScenario.ExecuteAsync();
}