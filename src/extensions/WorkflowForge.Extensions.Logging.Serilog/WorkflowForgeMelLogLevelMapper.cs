using Microsoft.Extensions.Logging;
using WorkflowForge.Operations;

namespace WorkflowForge.Extensions.Logging.Serilog
{
    internal static class WorkflowForgeMelLogLevelMapper
    {
        internal static LogLevel ToMelLevel(WorkflowForgeLogLevel level) => level switch
        {
            WorkflowForgeLogLevel.Trace => LogLevel.Trace,
            WorkflowForgeLogLevel.Debug => LogLevel.Debug,
            WorkflowForgeLogLevel.Information => LogLevel.Information,
            WorkflowForgeLogLevel.Warning => LogLevel.Warning,
            WorkflowForgeLogLevel.Error => LogLevel.Error,
            WorkflowForgeLogLevel.Critical => LogLevel.Critical,
            _ => LogLevel.Information
        };
    }
}
