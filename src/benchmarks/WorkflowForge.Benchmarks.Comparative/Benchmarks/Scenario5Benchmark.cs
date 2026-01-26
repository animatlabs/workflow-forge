using BenchmarkDotNet.Attributes;
using WorkflowForge.Benchmarks.Comparative.Implementations.Elsa;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 5, iterationCount: 25)]
[MarkdownExporter]
[HtmlExporter]
public class Scenario5Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;

    [Params(1, 4, 8)]
    public int ConcurrencyLevel { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var parameters = new ScenarioParameters { ConcurrencyLevel = ConcurrencyLevel };
        _workflowForgeScenario = new Scenario5_ConcurrentExecution_WorkflowForge(parameters);
        _workflowForgeScenario.SetupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario = new Scenario5_ConcurrentExecution_WorkflowCore(parameters);
        _workflowCoreScenario.SetupAsync().GetAwaiter().GetResult();
        _elsaScenario = new Scenario5_ConcurrentExecution_Elsa(parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _workflowForgeScenario.CleanupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario.CleanupAsync().GetAwaiter().GetResult();
        _elsaScenario.CleanupAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true, Description = "WorkflowForge - Concurrent Execution")]
    public async Task<ScenarioResult> WorkflowForge_ConcurrentExecution() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Concurrent Execution")]
    public async Task<ScenarioResult> WorkflowCore_ConcurrentExecution() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Concurrent Execution")]
    public async Task<ScenarioResult> Elsa_ConcurrentExecution() => await _elsaScenario.ExecuteAsync();
}