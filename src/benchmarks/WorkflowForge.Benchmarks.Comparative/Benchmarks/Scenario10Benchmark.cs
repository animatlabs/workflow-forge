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
public class Scenario10Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;

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
        _elsaScenario = new Scenario10_LongRunning_Elsa(parameters);
        _elsaScenario.SetupAsync().GetAwaiter().GetResult();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _workflowForgeScenario.CleanupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario.CleanupAsync().GetAwaiter().GetResult();
        _elsaScenario.CleanupAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true, Description = "WorkflowForge - Long Running")]
    public async Task<ScenarioResult> WorkflowForge_LongRunning() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Long Running")]
    public async Task<ScenarioResult> WorkflowCore_LongRunning() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Long Running")]
    public async Task<ScenarioResult> Elsa_LongRunning() => await _elsaScenario.ExecuteAsync();
}