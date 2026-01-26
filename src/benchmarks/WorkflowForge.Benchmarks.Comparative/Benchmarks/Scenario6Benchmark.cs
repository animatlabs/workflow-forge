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
public class Scenario6Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters();
        _workflowForgeScenario = new Scenario6_ErrorHandling_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario = new Scenario6_ErrorHandling_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = new Scenario6_ErrorHandling_Elsa(parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _workflowForgeScenario.CleanupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario.CleanupAsync().GetAwaiter().GetResult();
        _elsaScenario.CleanupAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true, Description = "WorkflowForge - Error Handling")]
    public async Task<ScenarioResult> WorkflowForge_ErrorHandling() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Error Handling")]
    public async Task<ScenarioResult> WorkflowCore_ErrorHandling() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Error Handling")]
    public async Task<ScenarioResult> Elsa_ErrorHandling() => await _elsaScenario.ExecuteAsync();
}