using BenchmarkDotNet.Attributes;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

public class Scenario6Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;
    private IWorkflowScenario _temporalScenario = null!;
    private IWorkflowScenario _daprScenario = null!;
    private IWorkflowScenario _workflowEngineNetScenario = null!;

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters();
        _workflowForgeScenario = new Scenario6_ErrorHandling_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario = new Scenario6_ErrorHandling_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = ElsaScenarioFactory.Create(6, parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();

        _temporalScenario = TemporalScenarioFactory.Create(6, parameters);
        _temporalScenario.SetupAsync().GetAwaiter().GetResult();
        _daprScenario = DaprScenarioFactory.Create(6, parameters);
        _daprScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowEngineNetScenario = WorkflowEngineNetScenarioFactory.Create(6, parameters);
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

    [Benchmark(Baseline = true, Description = "WorkflowForge - Error Handling")]
    public async Task<ScenarioResult> WorkflowForge_ErrorHandling() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Error Handling")]
    public async Task<ScenarioResult> WorkflowCore_ErrorHandling() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Error Handling")]
    public async Task<ScenarioResult> Elsa_ErrorHandling() => await _elsaScenario.ExecuteAsync();

    [Benchmark(Description = "Temporal - Error Handling")]
    public async Task<ScenarioResult> Temporal_ErrorHandling() => await _temporalScenario.ExecuteAsync();

    [Benchmark(Description = "Dapr - Error Handling")]
    public async Task<ScenarioResult> Dapr_ErrorHandling() => await _daprScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowEngineNet - Error Handling")]
    public async Task<ScenarioResult> WorkflowEngineNet_ErrorHandling() => await _workflowEngineNetScenario.ExecuteAsync();
}