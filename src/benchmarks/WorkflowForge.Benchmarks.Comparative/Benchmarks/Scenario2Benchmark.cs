using BenchmarkDotNet.Attributes;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

public class Scenario2Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;
    private IWorkflowScenario _temporalScenario = null!;
    private IWorkflowScenario _daprScenario = null!;
    private IWorkflowScenario _workflowEngineNetScenario = null!;

    [Params(5, 10, 25)]
    public int OperationCount { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters { OperationCount = OperationCount };
        _workflowForgeScenario = new Scenario2_DataPassing_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario = new Scenario2_DataPassing_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = ElsaScenarioFactory.Create(2, parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();

        _temporalScenario = TemporalScenarioFactory.Create(2, parameters);
        _temporalScenario.SetupAsync().GetAwaiter().GetResult();
        _daprScenario = DaprScenarioFactory.Create(2, parameters);
        _daprScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowEngineNetScenario = WorkflowEngineNetScenarioFactory.Create(2, parameters);
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

    [Benchmark(Baseline = true, Description = "WorkflowForge - Data Passing")]
    public async Task<ScenarioResult> WorkflowForge_DataPassing() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Data Passing")]
    public async Task<ScenarioResult> WorkflowCore_DataPassing() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Data Passing")]
    public async Task<ScenarioResult> Elsa_DataPassing() => await _elsaScenario.ExecuteAsync();

    [Benchmark(Description = "Temporal - Data Passing")]
    public async Task<ScenarioResult> Temporal_DataPassing() => await _temporalScenario.ExecuteAsync();

    [Benchmark(Description = "Dapr - Data Passing")]
    public async Task<ScenarioResult> Dapr_DataPassing() => await _daprScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowEngineNet - Data Passing")]
    public async Task<ScenarioResult> WorkflowEngineNet_DataPassing() => await _workflowEngineNetScenario.ExecuteAsync();
}