#if !NET48
using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Model.Builder;
using WorkflowForge.Benchmarks.Comparative.Scenarios;

namespace WorkflowForge.Benchmarks.Comparative.Implementations.WorkflowEngineNet;

/// <summary>
/// Infrastructure helpers for WorkflowEngine.NET (OptimaJet) scenarios.
/// NOTE: OptimaJet WorkflowEngine.NET requires an external database for persistence
/// (MSSQL, PostgreSQL, SQLite, MySQL, etc.). There is no built-in in-memory provider.
/// These benchmarks use the real ProcessDefinitionBuilder API for scheme DEFINITION,
/// and a lightweight in-memory state machine for EXECUTION.
/// See BENCHMARK_EXCLUSIONS.md for details.
/// </summary>
public static class WorkflowEngineNetInfrastructure
{
    public static ScenarioResult NotAvailableResult(string scenarioName) => new()
    {
        Success = true,
        OperationsExecuted = 0,
        OutputData = $"WorkflowEngine.NET ({scenarioName}): scheme built with ProcessDefinitionBuilder. Full runtime requires external DB.",
        Metadata =
        {
            ["FrameworkName"] = "WorkflowEngineNet",
            ["Available"] = "partial",
            ["Mode"] = "StateMachineSimulation",
            ["SchemeBuiltWith"] = "ProcessDefinitionBuilder",
            ["PersistenceNote"] = "OptimaJet requires external DB (MSSQL/PostgreSQL/SQLite) for full runtime"
        }
    };

    /// <summary>
    /// Builds a linear workflow scheme: Start → Step1 → ... → StepN → Complete
    /// using the real ProcessDefinitionBuilder API.
    /// </summary>
    public static ProcessDefinition BuildLinearScheme(string name, int stepCount)
    {
        var builder = ProcessDefinitionBuilder.Create(name);

        ActivityDefinition startActivity;
        builder.CreateActivity("Start").Initial().Ref(out startActivity);

        CommandDefinition nextCmd, finishCmd;
        builder.CreateOrUpdateCommand("Next").Ref(out nextCmd);
        builder.CreateOrUpdateCommand("Finish").Ref(out finishCmd);

        var previousActivity = startActivity;
        for (int i = 1; i <= stepCount; i++)
        {
            ActivityDefinition stepActivity;
            builder.CreateActivity($"Step{i}").Ref(out stepActivity);
            builder.CreateTransition($"To_Step{i}", previousActivity, stepActivity)
                .TriggeredByCommand(nextCmd);
            previousActivity = stepActivity;
        }

        ActivityDefinition finalActivity;
        builder.CreateActivity("Complete").Final().Ref(out finalActivity);
        builder.CreateTransition("To_Complete", previousActivity, finalActivity)
            .TriggeredByCommand(finishCmd);

        return builder.ProcessDefinition;
    }

    /// <summary>
    /// Builds a branching workflow scheme: Start → (BranchTrue | BranchFalse) → Complete
    /// </summary>
    public static ProcessDefinition BuildBranchingScheme(string name)
    {
        var builder = ProcessDefinitionBuilder.Create(name);

        ActivityDefinition start, branchTrue, branchFalse, complete;
        CommandDefinition goTrue, goFalse, finish;

        builder.CreateActivity("Start").Initial().Ref(out start);
        builder.CreateActivity("BranchTrue").Ref(out branchTrue);
        builder.CreateActivity("BranchFalse").Ref(out branchFalse);
        builder.CreateActivity("Complete").Final().Ref(out complete);
        builder.CreateOrUpdateCommand("GoTrue").Ref(out goTrue);
        builder.CreateOrUpdateCommand("GoFalse").Ref(out goFalse);
        builder.CreateOrUpdateCommand("Finish").Ref(out finish);

        builder.CreateTransition("To_BranchTrue", start, branchTrue).TriggeredByCommand(goTrue);
        builder.CreateTransition("To_BranchFalse", start, branchFalse).TriggeredByCommand(goFalse);
        builder.CreateTransition("True_To_Complete", branchTrue, complete).TriggeredByCommand(finish);
        builder.CreateTransition("False_To_Complete", branchFalse, complete).TriggeredByCommand(finish);

        return builder.ProcessDefinition;
    }

    /// <summary>
    /// Builds a parallel scheme: Start → Branch0..N → Complete (sequential approximation).
    /// WorkflowEngine.NET parallel gateways require full runtime; sequential branches used here.
    /// </summary>
    public static ProcessDefinition BuildParallelScheme(string name, int branchCount)
    {
        var builder = ProcessDefinitionBuilder.Create(name);

        ActivityDefinition start;
        builder.CreateActivity("Start").Initial().Ref(out start);
        CommandDefinition nextCmd;
        builder.CreateOrUpdateCommand("Next").Ref(out nextCmd);

        var previous = start;
        for (int i = 0; i < branchCount; i++)
        {
            ActivityDefinition branch;
            builder.CreateActivity($"Branch{i}").Ref(out branch);
            builder.CreateTransition($"To_Branch{i}", previous, branch).TriggeredByCommand(nextCmd);
            previous = branch;
        }

        ActivityDefinition complete;
        builder.CreateActivity("Complete").Final().Ref(out complete);
        CommandDefinition finishCmd;
        builder.CreateOrUpdateCommand("Finish").Ref(out finishCmd);
        builder.CreateTransition("To_Complete", previous, complete).TriggeredByCommand(finishCmd);

        return builder.ProcessDefinition;
    }
}

/// <summary>
/// In-memory state machine that mirrors WorkflowEngine.NET state machine semantics.
/// Used for executing workflows built with ProcessDefinitionBuilder without an external DB.
/// </summary>
public class WorkflowState
{
    private readonly ProcessDefinition _definition;
    private ActivityDefinition _currentActivity;
    private int _stepsExecuted;
    private readonly Dictionary<string, object> _data = new();

    public int StepsExecuted => _stepsExecuted;
    public bool IsComplete => _currentActivity.IsFinal;
    public string CurrentActivityName => _currentActivity.Name;
    public Dictionary<string, object> Data => _data;

    public WorkflowState(ProcessDefinition definition)
    {
        _definition = definition;
        _currentActivity = definition.InitialActivity
            ?? definition.Activities.FirstOrDefault(a => a.IsInitial)
            ?? definition.Activities.First();
    }

    public Task ExecuteNextCommandAsync()
    {
        var transition = _definition.Transitions
            .FirstOrDefault(t => t.From.Name == _currentActivity.Name);

        if (transition != null)
        {
            _currentActivity = transition.To;
            _stepsExecuted++;
        }
        return Task.CompletedTask;
    }

    public Task ExecuteFinishCommandAsync() => ExecuteNextCommandAsync();

    public Task ExecuteCommandAsync(string commandName)
    {
        var transition = _definition.Transitions
            .FirstOrDefault(t => t.From.Name == _currentActivity.Name
                && t.Trigger?.Command?.Name == commandName);

        if (transition != null)
        {
            _currentActivity = transition.To;
            _stepsExecuted++;
        }
        return Task.CompletedTask;
    }

    public IEnumerable<string> GetAvailableCommands()
    {
        return _definition.Transitions
            .Where(t => t.From.Name == _currentActivity.Name && t.Trigger?.Command != null)
            .Select(t => t.Trigger.Command!.Name)
            .Distinct();
    }
}
#endif
