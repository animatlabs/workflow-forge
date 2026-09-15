using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Observability.OpenTelemetry
{
    /// <summary>
    /// Creates one span per operation and records operation metrics, honouring the
    /// <see cref="WorkflowForgeOpenTelemetryOptions"/> the foundry was enabled with.
    /// </summary>
    public sealed class OpenTelemetryOperationMiddleware : IWorkflowOperationMiddleware
    {
        private readonly WorkflowForgeOpenTelemetryService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenTelemetryOperationMiddleware"/> class.
        /// </summary>
        /// <param name="service">The telemetry service that owns the activity source and meter.</param>
        /// <exception cref="ArgumentNullException">Thrown when service is null.</exception>
        public OpenTelemetryOperationMiddleware(WorkflowForgeOpenTelemetryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc />
        public async Task<object?> ExecuteAsync(
            IWorkflowOperation operation,
            IWorkflowFoundry foundry,
            object? inputData,
            Func<CancellationToken, Task<object?>> next,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            var operationName = operation.Name;
            using var activity = _service.StartActivity(operationName, ActivityKind.Internal);
            if (activity != null)
            {
                activity.SetTag("workflowforge.operation.id", operation.Id);
                activity.SetTag("workflowforge.operation.name", operationName);
                activity.SetTag("workflowforge.execution.id", foundry?.ExecutionId);
                activity.SetTag("workflowforge.workflow.name", foundry?.CurrentWorkflow?.Name);
            }

            _service.IncrementActiveOperations(operationName);
            var timestamp = Stopwatch.GetTimestamp();
            var success = false;

            try
            {
                var result = await next(cancellationToken).ConfigureAwait(false);
                success = true;
                activity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("exception.type", ex.GetType().FullName);
                throw;
            }
            finally
            {
                _service.DecrementActiveOperations(operationName);
                _service.RecordOperation(operationName, Elapsed(timestamp), success);
            }
        }

        private static TimeSpan Elapsed(long startTimestamp)
        {
            var ticks = Stopwatch.GetTimestamp() - startTimestamp;
            return TimeSpan.FromSeconds((double)ticks / Stopwatch.Frequency);
        }
    }
}
