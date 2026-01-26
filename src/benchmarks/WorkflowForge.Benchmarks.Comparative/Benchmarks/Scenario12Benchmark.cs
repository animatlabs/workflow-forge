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
public class Scenario12Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;

    [Params(1, 5)]
    public int DelayMilliseconds { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters
        {
            DelayMilliseconds = DelayMilliseconds
        };
        _workflowForgeScenario = new Scenario12_EventDriven_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario = new Scenario12_EventDriven_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = new Scenario12_EventDriven_Elsa(parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _workflowForgeScenario.CleanupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario.CleanupAsync().GetAwaiter().GetResult();
        _elsaScenario.CleanupAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true, Description = "WorkflowForge - Event-Driven")]
    public async Task<ScenarioResult> WorkflowForge_EventDriven() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Event-Driven")]
    public async Task<ScenarioResult> WorkflowCore_EventDriven() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Event-Driven")]
    public async Task<ScenarioResult> Elsa_EventDriven() => await _elsaScenario.ExecuteAsync();
}