using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using Xunit;

namespace WorkflowForge.Extensions.Observability.OpenTelemetry.Tests;

public class OpenTelemetryMiddlewareShould : IDisposable
{
    private readonly string _serviceName = "OTelMiddleware-" + Guid.NewGuid().ToString("N");
    private readonly List<Activity> _activities = new();
    private readonly ActivityListener _listener;

    public OpenTelemetryMiddlewareShould()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == _serviceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                lock (_activities)
                {
                    _activities.Add(a);
                }
            }
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    private Activity[] Captured()
    {
        lock (_activities)
        {
            return _activities.ToArray();
        }
    }

    private WorkflowForgeOpenTelemetryService NewService(bool tracing = true) =>
        new(new WorkflowForgeOpenTelemetryOptions { ServiceName = _serviceName, EnableTracing = tracing });

    [Fact]
    public void ThrowArgumentNullException_GivenNullServiceToOperationMiddleware()
        => Assert.Throws<ArgumentNullException>(() => new OpenTelemetryOperationMiddleware(null!));

    [Fact]
    public void ThrowArgumentNullException_GivenNullServiceToWorkflowMiddleware()
        => Assert.Throws<ArgumentNullException>(() => new OpenTelemetryWorkflowMiddleware(null!));

    [Fact]
    public async Task ThrowArgumentNullException_GivenNullOperation()
    {
        using var service = NewService();
        var middleware = new OpenTelemetryOperationMiddleware(service);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            middleware.ExecuteAsync(null!, new FakeFoundry(), null, _ => Task.FromResult<object?>(null)));
    }

    [Fact]
    public async Task ThrowArgumentNullException_GivenNullNextOnOperationMiddleware()
    {
        using var service = NewService();
        var middleware = new OpenTelemetryOperationMiddleware(service);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            middleware.ExecuteAsync(new NoOpOperation(), new FakeFoundry(), null, null!));
    }

    [Fact]
    public async Task ThrowArgumentNullException_GivenNullWorkflow()
    {
        using var service = NewService();
        var middleware = new OpenTelemetryWorkflowMiddleware(service);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            middleware.ExecuteAsync(null!, new FakeFoundry(), () => Task.CompletedTask));
    }

    [Fact]
    public async Task ThrowArgumentNullException_GivenNullNextOnWorkflowMiddleware()
    {
        using var service = NewService();
        var middleware = new OpenTelemetryWorkflowMiddleware(service);
        var workflow = WorkflowForge.CreateWorkflow("W").AddOperation("a", (_, _) => Task.CompletedTask).Build();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            middleware.ExecuteAsync(workflow, new FakeFoundry(), null!));
    }

    [Fact]
    public async Task MarkTheWorkflowSpanFailed_GivenTheWorkflowThrows()
    {
        using var service = NewService();
        var middleware = new OpenTelemetryWorkflowMiddleware(service);
        var workflow = WorkflowForge.CreateWorkflow("FailingWorkflow")
            .AddOperation("a", (_, _) => Task.CompletedTask)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.ExecuteAsync(workflow, new FakeFoundry(), () => throw new InvalidOperationException("nope")));

        var span = Assert.Single(Captured());
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Equal(typeof(InvalidOperationException).FullName, span.GetTagItem("exception.type"));
    }

    [Fact]
    public async Task StillRunTheWorkflow_GivenTracingDisabled()
    {
        using var service = NewService(tracing: false);
        var middleware = new OpenTelemetryWorkflowMiddleware(service);
        var workflow = WorkflowForge.CreateWorkflow("NoTracing")
            .AddOperation("a", (_, _) => Task.CompletedTask)
            .Build();

        var ran = false;
        await middleware.ExecuteAsync(workflow, new FakeFoundry(), () =>
        {
            ran = true;
            return Task.CompletedTask;
        });

        Assert.True(ran);
        Assert.Empty(Captured());
    }

    [Fact]
    public async Task TagTheWorkflowSpan_GivenASuccessfulWorkflow()
    {
        using var service = NewService();
        var middleware = new OpenTelemetryWorkflowMiddleware(service);
        var workflow = WorkflowForge.CreateWorkflow("TaggedWorkflow")
            .AddOperation("a", (_, _) => Task.CompletedTask)
            .Build();
        var foundry = new FakeFoundry();

        await middleware.ExecuteAsync(workflow, foundry, () => Task.CompletedTask);

        var span = Assert.Single(Captured());
        Assert.Equal(ActivityStatusCode.Ok, span.Status);
        Assert.Equal(workflow.Id, span.GetTagItem("workflowforge.workflow.id"));
        Assert.Equal("TaggedWorkflow", span.GetTagItem("workflowforge.workflow.name"));
        Assert.Equal(foundry.ExecutionId, span.GetTagItem("workflowforge.execution.id"));
    }

    private sealed class NoOpOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();

        public string Name => "NoOp";

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            => Task.FromResult(inputData);

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Dispose()
        {
        }
    }

    private sealed class FakeFoundry : Testing.FakeWorkflowFoundry
    {
    }
}
