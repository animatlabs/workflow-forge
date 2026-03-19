using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Benchmarks;

internal static class WorkflowEngineNetScenarioFactory
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
        return new WorkflowEngineNetNotSupportedScenario(scenarioNumber);
#else
        return scenarioNumber switch
        {
            1 => new Implementations.WorkflowEngineNet.Scenario1_SimpleSequential_WorkflowEngineNet(parameters),
            2 => new Implementations.WorkflowEngineNet.Scenario2_DataPassing_WorkflowEngineNet(parameters),
            3 => new Implementations.WorkflowEngineNet.Scenario3_ConditionalBranching_WorkflowEngineNet(parameters),
            4 => new Implementations.WorkflowEngineNet.Scenario4_LoopProcessing_WorkflowEngineNet(parameters),
            5 => new Implementations.WorkflowEngineNet.Scenario5_ConcurrentExecution_WorkflowEngineNet(parameters),
            6 => new Implementations.WorkflowEngineNet.Scenario6_ErrorHandling_WorkflowEngineNet(parameters),
            7 => new Implementations.WorkflowEngineNet.Scenario7_CreationOverhead_WorkflowEngineNet(parameters),
            8 => new Implementations.WorkflowEngineNet.Scenario8_CompleteLifecycle_WorkflowEngineNet(parameters),
            9 => new Implementations.WorkflowEngineNet.Scenario9_StateMachine_WorkflowEngineNet(parameters),
            10 => new Implementations.WorkflowEngineNet.Scenario10_LongRunning_WorkflowEngineNet(parameters),
            11 => new Implementations.WorkflowEngineNet.Scenario11_ParallelExecution_WorkflowEngineNet(parameters),
            12 => new Implementations.WorkflowEngineNet.Scenario12_EventDriven_WorkflowEngineNet(parameters),
            _ => throw new ArgumentException($"Unknown scenario: {scenarioNumber}")
        };
#endif
    }
}

internal sealed class WorkflowEngineNetNotSupportedScenario : IWorkflowScenario
{
    private static readonly ScenarioResult SkipResult = new()
    {
        Success = true,
        OutputData = "WorkflowEngine.NET not supported on .NET Framework 4.8",
        OperationsExecuted = 0
    };

    public string Name { get; }
    public string Description => "WorkflowEngine.NET (skipped - not supported on this framework)";

    internal WorkflowEngineNetNotSupportedScenario(int scenarioNumber)
    {
        Name = $"Scenario{scenarioNumber}_WorkflowEngineNet_NotSupported";
    }

    public Task SetupAsync() => Task.CompletedTask;
    public Task<ScenarioResult> ExecuteAsync() => Task.FromResult(SkipResult);
    public Task CleanupAsync() => Task.CompletedTask;
}
