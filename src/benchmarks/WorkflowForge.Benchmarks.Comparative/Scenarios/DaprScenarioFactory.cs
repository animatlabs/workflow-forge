using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

internal static class DaprScenarioFactory
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
        return new DaprNotSupportedScenario(scenarioNumber);
#else
        return scenarioNumber switch
        {
            1 => new Implementations.Dapr.Scenario1_SimpleSequential_Dapr(parameters),
            2 => new Implementations.Dapr.Scenario2_DataPassing_Dapr(parameters),
            3 => new Implementations.Dapr.Scenario3_ConditionalBranching_Dapr(parameters),
            4 => new Implementations.Dapr.Scenario4_LoopProcessing_Dapr(parameters),
            5 => new Implementations.Dapr.Scenario5_ConcurrentExecution_Dapr(parameters),
            6 => new Implementations.Dapr.Scenario6_ErrorHandling_Dapr(parameters),
            7 => new Implementations.Dapr.Scenario7_CreationOverhead_Dapr(parameters),
            8 => new Implementations.Dapr.Scenario8_CompleteLifecycle_Dapr(parameters),
            9 => new Implementations.Dapr.Scenario9_StateMachine_Dapr(parameters),
            10 => new Implementations.Dapr.Scenario10_LongRunning_Dapr(parameters),
            11 => new Implementations.Dapr.Scenario11_ParallelExecution_Dapr(parameters),
            12 => new Implementations.Dapr.Scenario12_EventDriven_Dapr(parameters),
            _ => throw new ArgumentException($"Unknown scenario: {scenarioNumber}")
        };
#endif
    }
}

internal sealed class DaprNotSupportedScenario : IWorkflowScenario
{
    private static readonly ScenarioResult SkipResult = new()
    {
        Success = true,
        OutputData = "Dapr not supported on .NET Framework 4.8",
        OperationsExecuted = 0
    };

    public string Name { get; }
    public string Description => "Dapr (skipped - not supported on this framework)";

    internal DaprNotSupportedScenario(int scenarioNumber)
    {
        Name = $"Scenario{scenarioNumber}_Dapr_NotSupported";
    }

    public Task SetupAsync() => Task.CompletedTask;
    public Task<ScenarioResult> ExecuteAsync() => Task.FromResult(SkipResult);
    public Task CleanupAsync() => Task.CompletedTask;
}
