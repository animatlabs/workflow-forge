using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

internal static class ElsaScenarioFactory
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
        return new ElsaNotSupportedScenario(scenarioNumber);
#else
        return scenarioNumber switch
        {
            1 => new Implementations.Elsa.Scenario1_SimpleSequential_Elsa(parameters),
            2 => new Implementations.Elsa.Scenario2_DataPassing_Elsa(parameters),
            3 => new Implementations.Elsa.Scenario3_ConditionalBranching_Elsa(parameters),
            4 => new Implementations.Elsa.Scenario4_LoopProcessing_Elsa(parameters),
            5 => new Implementations.Elsa.Scenario5_ConcurrentExecution_Elsa(parameters),
            6 => new Implementations.Elsa.Scenario6_ErrorHandling_Elsa(parameters),
            7 => new Implementations.Elsa.Scenario7_CreationOverhead_Elsa(parameters),
            8 => new Implementations.Elsa.Scenario8_CompleteLifecycle_Elsa(parameters),
            9 => new Implementations.Elsa.Scenario9_StateMachine_Elsa(parameters),
            10 => new Implementations.Elsa.Scenario10_LongRunning_Elsa(parameters),
            11 => new Implementations.Elsa.Scenario11_ParallelExecution_Elsa(parameters),
            12 => new Implementations.Elsa.Scenario12_EventDriven_Elsa(parameters),
            _ => throw new ArgumentException($"Unknown scenario: {scenarioNumber}")
        };
#endif
    }
}

internal sealed class ElsaNotSupportedScenario : IWorkflowScenario
{
    private static readonly ScenarioResult SkipResult = new()
    {
        Success = true,
        OutputData = "Elsa not supported on .NET Framework 4.8",
        OperationsExecuted = 0
    };

    public string Name { get; }
    public string Description => "Elsa (skipped - not supported on this framework)";

    internal ElsaNotSupportedScenario(int scenarioNumber)
    {
        Name = $"Scenario{scenarioNumber}_Elsa_NotSupported";
    }

    public Task SetupAsync() => Task.CompletedTask;

    public Task<ScenarioResult> ExecuteAsync() => Task.FromResult(SkipResult);

    public Task CleanupAsync() => Task.CompletedTask;
}