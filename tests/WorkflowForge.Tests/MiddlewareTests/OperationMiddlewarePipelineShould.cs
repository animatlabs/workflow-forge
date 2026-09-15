using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.MiddlewareTests
{
    public class OperationMiddlewarePipelineShould
    {
        [Fact]
        public async Task ReRunInnerMiddleware_GivenOuterMiddlewareInvokesNextTwice()
        {
            var innerInvocations = 0;
            using var foundry = WorkflowForge.CreateFoundry("RetryPipeline");
            foundry.AddMiddleware(new RetryingMiddleware(attempts: 2));
            foundry.AddMiddleware(new CountingMiddleware(() => innerInvocations++));
            foundry.AddOperation(new DelegateWorkflowOperation("Work", (_, _, _) => Task.FromResult<object?>("done")));

            await foundry.ForgeAsync();

            Assert.Equal(2, innerInvocations);
        }

        [Fact]
        public async Task ExecuteOperationOncePerAttempt_GivenOuterMiddlewareInvokesNextTwice()
        {
            var operationInvocations = 0;
            using var foundry = WorkflowForge.CreateFoundry("RetryPipelineOperation");
            foundry.AddMiddleware(new RetryingMiddleware(attempts: 3));
            foundry.AddOperation(new DelegateWorkflowOperation("Work", (_, _, _) =>
            {
                operationInvocations++;
                return Task.FromResult<object?>("done");
            }));

            await foundry.ForgeAsync();

            Assert.Equal(3, operationInvocations);
        }

        [Fact]
        public async Task PreserveLayerOrder_GivenMultipleAttempts()
        {
            var order = new List<string>();
            using var foundry = WorkflowForge.CreateFoundry("OrderedPipeline");
            foundry.AddMiddleware(new RetryingMiddleware(attempts: 2));
            foundry.AddMiddleware(new CountingMiddleware(() => order.Add("outer")));
            foundry.AddMiddleware(new CountingMiddleware(() => order.Add("inner")));
            foundry.AddOperation(new DelegateWorkflowOperation("Work", (_, _, _) => Task.FromResult<object?>(null)));

            await foundry.ForgeAsync();

            Assert.Equal(new[] { "outer", "inner", "outer", "inner" }, order);
        }

        private sealed class RetryingMiddleware : IWorkflowOperationMiddleware
        {
            private readonly int _attempts;

            public RetryingMiddleware(int attempts) => _attempts = attempts;

            public async Task<object?> ExecuteAsync(
                IWorkflowOperation operation,
                IWorkflowFoundry foundry,
                object? inputData,
                Func<CancellationToken, Task<object?>> next,
                CancellationToken cancellationToken = default)
            {
                object? result = null;
                for (var attempt = 0; attempt < _attempts; attempt++)
                {
                    result = await next(cancellationToken).ConfigureAwait(false);
                }

                return result;
            }
        }

        private sealed class CountingMiddleware : IWorkflowOperationMiddleware
        {
            private readonly Action _onInvoked;

            public CountingMiddleware(Action onInvoked) => _onInvoked = onInvoked;

            public Task<object?> ExecuteAsync(
                IWorkflowOperation operation,
                IWorkflowFoundry foundry,
                object? inputData,
                Func<CancellationToken, Task<object?>> next,
                CancellationToken cancellationToken = default)
            {
                _onInvoked();
                return next(cancellationToken);
            }
        }
    }
}
