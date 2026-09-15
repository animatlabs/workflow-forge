namespace WorkflowForge.Tests.LoggersTests;

/// <summary>
/// Serialises tests that redirect <c>Console.Out</c>, which is process-wide state.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleOutputCollection
{
    public const string Name = "ConsoleOutput";
}
