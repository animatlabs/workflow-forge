using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowCore;
using WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowForge;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net48, warmupCount: 5, iterationCount: 50)]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 5, iterationCount: 50)]
[SimpleJob(RuntimeMoniker.Net10_0, warmupCount: 5, iterationCount: 50)]
[MarkdownExporter]
[HtmlExporter]
public class Scenario7Benchmark
{
    private IWorkflowScenario _workflowForgeScenario = null!;
    private IWorkflowScenario _workflowCoreScenario = null!;
    private IWorkflowScenario _elsaScenario = null!;

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
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _workflowForgeScenario.CleanupAsync().GetAwaiter().GetResult();
        _workflowCoreScenario.CleanupAsync().GetAwaiter().GetResult();
        _elsaScenario.CleanupAsync().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true, Description = "WorkflowForge - Creation Overhead")]
    public async Task<ScenarioResult> WorkflowForge_CreationOverhead() => await _workflowForgeScenario.ExecuteAsync();

    [Benchmark(Description = "WorkflowCore - Creation Overhead")]
    public async Task<ScenarioResult> WorkflowCore_CreationOverhead() => await _workflowCoreScenario.ExecuteAsync();

    [Benchmark(Description = "Elsa - Creation Overhead")]
    public async Task<ScenarioResult> Elsa_CreationOverhead() => await _elsaScenario.ExecuteAsync();
}