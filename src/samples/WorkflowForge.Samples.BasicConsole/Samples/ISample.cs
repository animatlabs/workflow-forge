using System.Threading.Tasks;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Interface for all WorkflowForge samples.
/// Provides a consistent structure for runnable examples.
/// </summary>
public interface ISample
{
    /// <summary>
    /// Gets the name of the sample.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets a brief description of what this sample demonstrates.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Executes the sample workflow.
    /// </summary>
    /// <returns>A task representing the asynchronous execution.</returns>
    Task RunAsync();
} 