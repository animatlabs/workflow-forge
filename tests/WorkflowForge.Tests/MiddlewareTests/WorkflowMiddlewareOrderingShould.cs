using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Tests.MiddlewareTests;

/// <summary>
/// Workflow-level middleware follows the same Russian-doll rule as operation middleware:
/// first registered is the outermost layer.
/// </summary>
public class WorkflowMiddlewareOrderingShould
{
    [Fact]
    public async Task WrapInRegistrationOrder_GivenMultipleWorkflowMiddlewares()
    {
        var order = new List<string>();
        using var smith = WorkflowForge.CreateSmith();
        smith.AddWorkflowMiddleware(new RecordingWorkflowMiddleware("outer", order));
        smith.AddWorkflowMiddleware(new RecordingWorkflowMiddleware("inner", order));

        var workflow = WorkflowForge.CreateWorkflow("WorkflowMiddlewareOrder")
            .AddOperation("body", (_, _) =>
            {
                order.Add("body");
                return Task.CompletedTask;
            })
            .Build();

        await smith.ForgeAsync(workflow);

        Assert.Equal(
            new[] { "outer:before", "inner:before", "body", "inner:after", "outer:after" },
            order.ToArray());
    }

    [Fact]
    public async Task ShortCircuitTheWorkflow_GivenAMiddlewareDoesNotCallNext()
    {
        var order = new List<string>();
        using var smith = WorkflowForge.CreateSmith();
        smith.AddWorkflowMiddleware(new ShortCircuitingWorkflowMiddleware());
        smith.AddWorkflowMiddleware(new RecordingWorkflowMiddleware("inner", order));

        var workflow = WorkflowForge.CreateWorkflow("WorkflowMiddlewareShortCircuit")
            .AddOperation("body", (_, _) =>
            {
                order.Add("body");
                return Task.CompletedTask;
            })
            .Build();

        await smith.ForgeAsync(workflow);

        Assert.Empty(order);
    }

    private sealed class RecordingWorkflowMiddleware : IWorkflowMiddleware
    {
        private readonly string _name;
        private readonly List<string> _order;

        public RecordingWorkflowMiddleware(string name, List<string> order)
        {
            _name = name;
            _order = order;
        }

        public async Task ExecuteAsync(IWorkflow workflow, IWorkflowFoundry foundry, Func<Task> next, CancellationToken cancellationToken = default)
        {
            _order.Add(_name + ":before");
            await next().ConfigureAwait(false);
            _order.Add(_name + ":after");
        }
    }

    private sealed class ShortCircuitingWorkflowMiddleware : IWorkflowMiddleware
    {
        public Task ExecuteAsync(IWorkflow workflow, IWorkflowFoundry foundry, Func<Task> next, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
