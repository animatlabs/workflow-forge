using Serilog.Events;
using WorkflowForge.Operations;

namespace WorkflowForge.Extensions.Logging.Serilog
{
    internal static class WorkflowForgeSerilogLogLevelMapper
    {
        internal static LogEventLevel ToSerilogLevel(WorkflowForgeLogLevel level) => level switch
        {
            WorkflowForgeLogLevel.Trace => LogEventLevel.Verbose,
            WorkflowForgeLogLevel.Debug => LogEventLevel.Debug,
            WorkflowForgeLogLevel.Information => LogEventLevel.Information,
            WorkflowForgeLogLevel.Warning => LogEventLevel.Warning,
            WorkflowForgeLogLevel.Error => LogEventLevel.Error,
            WorkflowForgeLogLevel.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}
