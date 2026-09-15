using System;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Extensions
{
    internal static class WorkflowForgeLogLevelParsing
    {
        internal static WorkflowForgeLogLevel ParseMinimumLevel(string? minimumLevel)
        {
            if (string.IsNullOrWhiteSpace(minimumLevel))
                return WorkflowForgeLogLevel.Information;

            return Enum.TryParse<WorkflowForgeLogLevel>(minimumLevel, ignoreCase: true, out var parsed)
                ? parsed
                : WorkflowForgeLogLevel.Information;
        }

        internal static bool IsLevelEnabled(IWorkflowForgeLogger logger, WorkflowForgeLogLevel level, WorkflowForgeLogLevel minimumLevel)
        {
            if (level < minimumLevel)
                return false;

            return logger.IsEnabled(level);
        }
    }
}
