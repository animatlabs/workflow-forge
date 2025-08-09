using System;

namespace WorkflowForge.Abstractions
{
    /// <summary>
    /// Default implementation of <see cref="ISystemTimeProvider"/> that provides real system time.
    /// </summary>
    public sealed class SystemTimeProvider : ISystemTimeProvider
    {
        /// <summary>
        /// Gets the singleton instance of the system time provider.
        /// </summary>
        public static readonly SystemTimeProvider Instance = new();

        // Private constructor to enforce singleton pattern
        private SystemTimeProvider()
        { }

        /// <inheritdoc />
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public DateTimeOffset Now => DateTimeOffset.Now;

        /// <inheritdoc />
        public DateTimeOffset Today => DateTimeOffset.Now.Date;
    }
}