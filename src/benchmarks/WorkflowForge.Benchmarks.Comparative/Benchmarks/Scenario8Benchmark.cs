using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

/// <summary>
/// Scenario 8: Complete Lifecycle (Register → Execute → Cleanup)
///
/// NOTE: WorkflowCore is intentionally excluded from this scenario due to architectural incompatibility.
/// WorkflowCore's background thread model (IWorkflowHost.Start() spins up worker threads) adds significant
/// overhead to repeated lifecycle operations, making it inappropriate for this specific benchmark.
/// This is a design trade-off in WorkflowCore's architecture, not a flaw - their threading model
/// is optimized for long-running workflows, not rapid create/destroy cycles.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net48, warmupCount: 5, iterationCount: 50)]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 5, iterationCount: 50)]
[SimpleJob(RuntimeMoniker.Net10_0, warmupCount: 5, iterationCount: 50)]
[MarkdownExporter]
[HtmlExporter]
public class Scenario8Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters();
        _workflowForgeScenario = new Scenario8_CompleteLifecycle_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = ElsaScenarioFactory.Create(8, parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _workflowForgeScenario.CleanupAsync().GetAwaiter().GetResult();
        _elsaScenario.CleanupAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true, Description = "WorkflowForge - Complete Lifecycle")]
    public async Task<ScenarioResult> WorkflowForge_CompleteLifecycle() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Complete Lifecycle")]
    public async Task<ScenarioResult> Elsa_CompleteLifecycle() => await _elsaScenario.ExecuteAsync();
}