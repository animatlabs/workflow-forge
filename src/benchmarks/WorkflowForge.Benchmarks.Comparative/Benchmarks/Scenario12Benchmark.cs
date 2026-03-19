using BenchmarkDotNet.Attributes;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

public class Scenario12Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;
    private IWorkflowScenario _temporalScenario = null!;
    private IWorkflowScenario _daprScenario = null!;
    private IWorkflowScenario _workflowEngineNetScenario = null!;

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
        _elsaScenario = ElsaScenarioFactory.Create(12, parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();

        _temporalScenario = TemporalScenarioFactory.Create(12, parameters);
        _temporalScenario.SetupAsync().GetAwaiter().GetResult();
        _daprScenario = DaprScenarioFactory.Create(12, parameters);
        _daprScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowEngineNetScenario = WorkflowEngineNetScenarioFactory.Create(12, parameters);
        _workflowEngineNetScenario.SetupAsync().GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _workflowForgeScenario.CleanupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario.CleanupAsync().GetAwaiter().GetResult();
        _elsaScenario.CleanupAsync().GetAwaiter().GetResult();
        _temporalScenario.CleanupAsync().GetAwaiter().GetResult();
        _daprScenario.CleanupAsync().GetAwaiter().GetResult();
        _workflowEngineNetScenario.CleanupAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true, Description = "WorkflowForge - Event-Driven")]
    public async Task<ScenarioResult> WorkflowForge_EventDriven() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Event-Driven")]
    public async Task<ScenarioResult> WorkflowCore_EventDriven() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Event-Driven")]
    public async Task<ScenarioResult> Elsa_EventDriven() => await _elsaScenario.ExecuteAsync();

    [Benchmark(Description = "Temporal - Event-Driven")]
    public async Task<ScenarioResult> Temporal_EventDriven() => await _temporalScenario.ExecuteAsync();

    [Benchmark(Description = "Dapr - Event-Driven")]
    public async Task<ScenarioResult> Dapr_EventDriven() => await _daprScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowEngineNet - Event-Driven")]
    public async Task<ScenarioResult> WorkflowEngineNet_EventDriven() => await _workflowEngineNetScenario.ExecuteAsync();
}
