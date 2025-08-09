using System;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// Provides an abstraction for system time operations to enable testability and time mocking.
    /// This is the single source of truth for all time-related operations in WorkflowForge.
    /// </summary>
    public interface ISystemTimeProvider
    {
        /// <summary>
        /// Gets the current date and time in Coordinated Universal Time (UTC).
        /// </summary>
        DateTimeOffset UtcNow { get; }

        /// <summary>
        /// Gets the current local date and time.
        /// </summary>
        DateTimeOffset Now { get; }

        /// <summary>
        /// Gets the current local date with the time component set to midnight.
        /// </summary>
        DateTimeOffset Today { get; }
    }
}