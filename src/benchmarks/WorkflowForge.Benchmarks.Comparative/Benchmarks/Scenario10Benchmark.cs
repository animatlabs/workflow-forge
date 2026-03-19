using BenchmarkDotNet.Attributes;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

public class Scenario10Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;
    private IWorkflowScenario _temporalScenario = null!;
    private IWorkflowScenario _daprScenario = null!;
    private IWorkflowScenario _workflowEngineNetScenario = null!;

    [Params(3, 5)]
    public int OperationCount { get; set; }

    [Params(1, 5)]
    public int DelayMilliseconds { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters
        {
            OperationCount = OperationCount,
            DelayMilliseconds = DelayMilliseconds
        };
        _workflowForgeScenario = new Scenario10_LongRunning_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario = new Scenario10_LongRunning_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = ElsaScenarioFactory.Create(10, parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();

        _temporalScenario = TemporalScenarioFactory.Create(10, parameters);
        _temporalScenario.SetupAsync().GetAwaiter().GetResult();
        _daprScenario = DaprScenarioFactory.Create(10, parameters);
        _daprScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowEngineNetScenario = WorkflowEngineNetScenarioFactory.Create(10, parameters);
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

    [Benchmark(Baseline = true, Description = "WorkflowForge - Long Running")]
    public async Task<ScenarioResult> WorkflowForge_LongRunning() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Long Running")]
    public async Task<ScenarioResult> WorkflowCore_LongRunning() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Long Running")]
    public async Task<ScenarioResult> Elsa_LongRunning() => await _elsaScenario.ExecuteAsync();

    [Benchmark(Description = "Temporal - Long Running")]
    public async Task<ScenarioResult> Temporal_LongRunning() => await _temporalScenario.ExecuteAsync();

    [Benchmark(Description = "Dapr - Long Running")]
    public async Task<ScenarioResult> Dapr_LongRunning() => await _daprScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowEngineNet - Long Running")]
    public async Task<ScenarioResult> WorkflowEngineNet_LongRunning() => await _workflowEngineNetScenario.ExecuteAsync();
}