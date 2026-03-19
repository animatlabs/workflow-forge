using BenchmarkDotNet.Attributes;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

public class Scenario7Benchmark
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
        _workflowForgeScenario = new Scenario7_CreationOverhead_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario = new Scenario7_CreationOverhead_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = ElsaScenarioFactory.Create(7, parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();

        _temporalScenario = TemporalScenarioFactory.Create(7, parameters);
        _temporalScenario.SetupAsync().GetAwaiter().GetResult();
        _daprScenario = DaprScenarioFactory.Create(7, parameters);
        _daprScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowEngineNetScenario = WorkflowEngineNetScenarioFactory.Create(7, parameters);
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

    [Benchmark(Baseline = true, Description = "WorkflowForge - Creation Overhead")]
    public async Task<ScenarioResult> WorkflowForge_CreationOverhead() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Creation Overhead")]
    public async Task<ScenarioResult> WorkflowCore_CreationOverhead() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Creation Overhead")]
    public async Task<ScenarioResult> Elsa_CreationOverhead() => await _elsaScenario.ExecuteAsync();

    [Benchmark(Description = "Temporal - Creation Overhead")]
    public async Task<ScenarioResult> Temporal_CreationOverhead() => await _temporalScenario.ExecuteAsync();

    [Benchmark(Description = "Dapr - Creation Overhead")]
    public async Task<ScenarioResult> Dapr_CreationOverhead() => await _daprScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowEngineNet - Creation Overhead")]
    public async Task<ScenarioResult> WorkflowEngineNet_CreationOverhead() => await _workflowEngineNetScenario.ExecuteAsync();
}