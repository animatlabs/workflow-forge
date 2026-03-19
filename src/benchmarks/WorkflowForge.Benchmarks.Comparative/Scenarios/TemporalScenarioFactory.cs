using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

internal static class TemporalScenarioFactory
{
    internal static bool IsSupported =>
#if NET48
        false;
#else
        true;
#endif

    internal static IWorkflowScenario Create(int scenarioNumber, ScenarioParameters parameters)
    {
#if NET48
        return new TemporalNotSupportedScenario(scenarioNumber);
#else
        return scenarioNumber switch
        {
            1 => new Implementations.Temporal.Scenario1_SimpleSequential_Temporal(parameters),
            2 => new Implementations.Temporal.Scenario2_DataPassing_Temporal(parameters),
            3 => new Implementations.Temporal.Scenario3_ConditionalBranching_Temporal(parameters),
            4 => new Implementations.Temporal.Scenario4_LoopProcessing_Temporal(parameters),
            5 => new Implementations.Temporal.Scenario5_ConcurrentExecution_Temporal(parameters),
            6 => new Implementations.Temporal.Scenario6_ErrorHandling_Temporal(parameters),
            7 => new Implementations.Temporal.Scenario7_CreationOverhead_Temporal(parameters),
            8 => new Implementations.Temporal.Scenario8_CompleteLifecycle_Temporal(parameters),
            9 => new Implementations.Temporal.Scenario9_StateMachine_Temporal(parameters),
            10 => new Implementations.Temporal.Scenario10_LongRunning_Temporal(parameters),
            11 => new Implementations.Temporal.Scenario11_ParallelExecution_Temporal(parameters),
            12 => new Implementations.Temporal.Scenario12_EventDriven_Temporal(parameters),
            _ => throw new ArgumentException($"Unknown scenario: {scenarioNumber}")
        };
#endif
    }
}

internal sealed class TemporalNotSupportedScenario : IWorkflowScenario
{
    private static readonly ScenarioResult SkipResult = new()
    {
        Success = true,
        OutputData = "Temporal not supported on .NET Framework 4.8",
        OperationsExecuted = 0
    };

    public string Name { get; }
    public string Description => "Temporal (skipped - not supported on this framework)";

    internal TemporalNotSupportedScenario(int scenarioNumber)
    {
        Name = $"Scenario{scenarioNumber}_Temporal_NotSupported";
    }

    public Task SetupAsync() => Task.CompletedTask;
    public Task<ScenarioResult> ExecuteAsync() => Task.FromResult(SkipResult);
    public Task CleanupAsync() => Task.CompletedTask;
}
