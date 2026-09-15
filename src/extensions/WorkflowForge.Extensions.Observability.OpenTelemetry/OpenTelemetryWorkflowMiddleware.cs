using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Observability.OpenTelemetry
{
    /// <summary>
    /// Creates the workflow-level span that operation spans nest under.
    /// Register it on the smith with <c>AddWorkflowMiddleware</c>.
    /// </summary>
    public sealed class OpenTelemetryWorkflowMiddleware : IWorkflowMiddleware
    {
        private readonly WorkflowForgeOpenTelemetryService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenTelemetryWorkflowMiddleware"/> class.
        /// </summary>
        /// <param name="service">The telemetry service that owns the activity source.</param>
        /// <exception cref="ArgumentNullException">Thrown when service is null.</exception>
        public OpenTelemetryWorkflowMiddleware(WorkflowForgeOpenTelemetryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(
            IWorkflow workflow,
            IWorkflowFoundry foundry,
            Func<Task> next,
            CancellationToken cancellationToken = default)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            using var activity = _service.StartActivity(workflow.Name, ActivityKind.Internal);
            if (activity != null)
            {
                activity.SetTag("workflowforge.workflow.id", workflow.Id);
                activity.SetTag("workflowforge.workflow.name", workflow.Name);
                activity.SetTag("workflowforge.execution.id", foundry?.ExecutionId);
            }

            try
            {
                await next().ConfigureAwait(false);
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("exception.type", ex.GetType().FullName);
                throw;
            }
        }
    }
}
