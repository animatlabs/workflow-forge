using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Middleware
{
    /// <summary>
    /// Invokes the operation middleware chain for a single operation, holding the per-operation
    /// context so it does not have to be captured again by every layer.
    /// </summary>
    /// <remarks>
    /// The position in the chain is a parameter rather than state, so a middleware may invoke
    /// <c>next</c> more than once — a retry re-runs every inner layer, not just the operation.
    /// </remarks>
    internal sealed class OperationMiddlewarePipelineState
    {
        private WorkflowFoundry _foundry = null!;
        private IWorkflowOperation _operation = null!;
        private object? _inputData;
        private IWorkflowOperationMiddleware[] _middleware = null!;

        public void Initialize(
            WorkflowFoundry foundry,
            IWorkflowOperation operation,
            object? inputData,
            IWorkflowOperationMiddleware[] middleware)
        {
            _foundry = foundry;
            _operation = operation;
            _inputData = inputData;
            _middleware = middleware;
        }

        public Task<object?> InvokeAsync(CancellationToken cancellationToken) => InvokeAsync(0, cancellationToken);

        private Task<object?> InvokeAsync(int index, CancellationToken cancellationToken)
        {
            if (index >= _middleware.Length)
            {
                return _operation.ForgeAsync(_inputData, _foundry, cancellationToken);
            }

            var middleware = _middleware[index];
            return middleware.ExecuteAsync(
                _operation,
                _foundry,
                _inputData,
                token => InvokeAsync(index + 1, token),
                cancellationToken);
        }
    }
}
