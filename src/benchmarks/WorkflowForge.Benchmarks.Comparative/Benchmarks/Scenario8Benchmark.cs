using BenchmarkDotNet.Attributes;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

/// <summary>
/// Scenario 8: Complete Lifecycle (Register -> Execute -> Cleanup)
///
/// NOTE: WorkflowCore is intentionally excluded from this scenario due to architectural incompatibility.
/// WorkflowCore's background thread model (IWorkflowHost.Start() spins up worker threads) adds significant
/// overhead to repeated lifecycle operations, making it inappropriate for this specific benchmark.
/// This is a design trade-off in WorkflowCore's architecture, not a flaw - their threading model
/// is optimized for long-running workflows, not rapid create/destroy cycles.
/// </summary>
public class Scenario8Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;
    private IWorkflowScenario _temporalScenario = null!;
    private IWorkflowScenario _daprScenario = null!;
    private IWorkflowScenario _workflowEngineNetScenario = null!;

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters();
        _workflowForgeScenario = new Scenario8_CompleteLifecycle_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = ElsaScenarioFactory.Create(8, parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();

        _temporalScenario = TemporalScenarioFactory.Create(8, parameters);
        _temporalScenario.SetupAsync().GetAwaiter().GetResult();
        _daprScenario = DaprScenarioFactory.Create(8, parameters);
        _daprScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowEngineNetScenario = WorkflowEngineNetScenarioFactory.Create(8, parameters);
        _workflowEngineNetScenario.SetupAsync().GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _workflowForgeScenario.CleanupAsync().GetAwaiter().GetResult();
        _elsaScenario.CleanupAsync().GetAwaiter().GetResult();
        _temporalScenario.CleanupAsync().GetAwaiter().GetResult();
        _daprScenario.CleanupAsync().GetAwaiter().GetResult();
        _workflowEngineNetScenario.CleanupAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true, Description = "WorkflowForge - Complete Lifecycle")]
    public async Task<ScenarioResult> WorkflowForge_CompleteLifecycle() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Complete Lifecycle")]
    public async Task<ScenarioResult> Elsa_CompleteLifecycle() => await _elsaScenario.ExecuteAsync();

    [Benchmark(Description = "Temporal - Complete Lifecycle")]
    public async Task<ScenarioResult> Temporal_CompleteLifecycle() => await _temporalScenario.ExecuteAsync();

    [Benchmark(Description = "Dapr - Complete Lifecycle")]
    public async Task<ScenarioResult> Dapr_CompleteLifecycle() => await _daprScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowEngineNet - Complete Lifecycle")]
    public async Task<ScenarioResult> WorkflowEngineNet_CompleteLifecycle() => await _workflowEngineNetScenario.ExecuteAsync();
}