namespace WorkflowForge.Benchmarks.Comparative.Scenarios;

/// <summary>
/// Base interface for all workflow scenarios.
/// Each scenario must be implemented for WorkflowForge, Workflow Core, and Elsa.
/// </summary>
public interface IWorkflowScenario
{
    /// <summary>
    /// Scenario name for reporting
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Scenario description
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Setup method called once before all iterations
    /// </summary>
    Task SetupAsync();

    /// <summary>
    /// Execute the workflow scenario
    /// </summary>
    Task<ScenarioResult> ExecuteAsync();

    /// <summary>
    /// Cleanup method called once after all iterations
    /// </summary>
    Task CleanupAsync();
}

/// <summary>
/// Result of scenario execution for validation
/// </summary>
public class ScenarioResult
{
    public bool Success { get; set; }
    public string? OutputData { get; set; }
    public int OperationsExecuted { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Common scenario parameters
/// </summary>
public class ScenarioParameters
{
    public int OperationCount { get; set; } = 10;
    public int ItemCount { get; set; } = 100;
    public int ConcurrencyLevel { get; set; } = 4;
    public bool IncludeLogging { get; set; } = false;
}