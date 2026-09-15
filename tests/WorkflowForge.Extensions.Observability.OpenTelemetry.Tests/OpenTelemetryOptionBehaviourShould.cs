using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using Xunit;

namespace WorkflowForge.Extensions.Observability.OpenTelemetry.Tests;

/// <summary>
/// Asserts that each telemetry option changes observable behaviour rather than only round-tripping.
/// </summary>
public class OpenTelemetryOptionBehaviourShould : IDisposable
{
    private readonly List<Activity> _activities = new();
    private readonly ActivityListener _listener;
    private readonly string _serviceName = "OTelBehaviour-" + Guid.NewGuid().ToString("N");

    public OpenTelemetryOptionBehaviourShould()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == _serviceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (_activities)
                {
                    _activities.Add(activity);
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

    private WorkflowForgeOpenTelemetryOptions Options(bool tracing = true, bool metrics = true, bool systemMetrics = true, bool operationMetrics = true)
        => new()
        {
            ServiceName = _serviceName,
            EnableTracing = tracing,
            EnableMetrics = metrics,
            EnableSystemMetrics = systemMetrics,
            EnableOperationMetrics = operationMetrics
        };

    [Fact]
    public async Task ProduceOneSpanPerOperation_GivenTracingEnabled()
    {
        var workflow = WorkflowForge.CreateWorkflow("OTelSpans")
            .AddOperation("one", (_, _) => Task.CompletedTask)
            .AddOperation("two", (_, _) => Task.CompletedTask)
            .AddOperation("three", (_, _) => Task.CompletedTask)
            .Build();

        using var foundry = WorkflowForge.CreateFoundry("OTelSpans");
        Assert.True(foundry.EnableOpenTelemetry(Options()));

        using var smith = WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        var spans = Captured();
        Assert.Equal(3, spans.Length);
        Assert.Equal(new[] { "one", "two", "three" }, spans.Select(a => a.DisplayName).ToArray());
        Assert.All(spans, a => Assert.Equal(ActivityStatusCode.Ok, a.Status));
    }

    [Fact]
    public async Task ProduceNoSpans_GivenTracingDisabled()
    {
        var workflow = WorkflowForge.CreateWorkflow("OTelNoSpans")
            .AddOperation("one", (_, _) => Task.CompletedTask)
            .Build();

        using var foundry = WorkflowForge.CreateFoundry("OTelNoSpans");
        Assert.True(foundry.EnableOpenTelemetry(Options(tracing: false)));

        using var smith = WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        Assert.Empty(Captured());
    }

    [Fact]
    public async Task MarkTheSpanFailed_GivenTheOperationThrows()
    {
        var workflow = WorkflowForge.CreateWorkflow("OTelFailure")
            .AddOperation("boom", (_, _) => throw new InvalidOperationException("nope"))
            .Build();

        using var foundry = WorkflowForge.CreateFoundry("OTelFailure");
        Assert.True(foundry.EnableOpenTelemetry(Options()));

        using var smith = WorkflowForge.CreateSmith();
        await Assert.ThrowsAnyAsync<Exception>(() => smith.ForgeAsync(workflow, foundry));

        var spans = Captured();
        Assert.Single(spans);
        Assert.Equal(ActivityStatusCode.Error, spans[0].Status);
    }

    [Fact]
    public async Task NestOperationSpansUnderTheWorkflowSpan_GivenTheWorkflowMiddleware()
    {
        var workflow = WorkflowForge.CreateWorkflow("OTelParent")
            .AddOperation("child", (_, _) => Task.CompletedTask)
            .Build();

        using var foundry = WorkflowForge.CreateFoundry("OTelParent");
        Assert.True(foundry.EnableOpenTelemetry(Options()));

        using var smith = WorkflowForge.CreateSmith();
        var workflowMiddleware = foundry.CreateOpenTelemetryWorkflowMiddleware();
        Assert.NotNull(workflowMiddleware);
        smith.AddWorkflowMiddleware(workflowMiddleware!);

        await smith.ForgeAsync(workflow, foundry);

        var spans = Captured();
        Assert.Equal(2, spans.Length);
        var child = spans.Single(a => a.DisplayName == "child");
        var parent = spans.Single(a => a.DisplayName == "OTelParent");
        Assert.Equal(parent.SpanId, child.ParentSpanId);
    }

    [Fact]
    public void RegisterNoSystemGauges_GivenSystemMetricsDisabled()
    {
        using var enabled = new WorkflowForgeOpenTelemetryService(Options());
        using var disabled = new WorkflowForgeOpenTelemetryService(Options(systemMetrics: false));

        Assert.True(enabled.SystemMetricsRegistered);
        Assert.False(disabled.SystemMetricsRegistered);
    }

    [Fact]
    public void RegisterNoSystemGauges_GivenMetricsDisabled()
    {
        using var service = new WorkflowForgeOpenTelemetryService(Options(metrics: false));

        Assert.False(service.SystemMetricsRegistered);
    }

    [Fact]
    public async Task RegisterTheOperationMiddlewareOnce_GivenEnableIsCalledTwice()
    {
        var workflow = WorkflowForge.CreateWorkflow("OTelIdempotent")
            .AddOperation("one", (_, _) => Task.CompletedTask)
            .Build();

        using var foundry = WorkflowForge.CreateFoundry("OTelIdempotent");
        Assert.True(foundry.EnableOpenTelemetry(Options()));
        Assert.False(foundry.EnableOpenTelemetry(Options()));

        using var smith = WorkflowForge.CreateSmith();
        await smith.ForgeAsync(workflow, foundry);

        Assert.Single(Captured());
    }

    [Fact]
    public void RemoveTheOperationMiddleware_GivenDisableAfterEnable()
    {
        using var foundry = WorkflowForge.CreateFoundry("OTelDisableRemovesMiddleware");

        Assert.True(foundry.EnableOpenTelemetry(Options()));
        Assert.Equal(1, MiddlewareCount(foundry));

        Assert.True(foundry.DisableOpenTelemetry());
        Assert.Equal(0, MiddlewareCount(foundry));

        Assert.True(foundry.EnableOpenTelemetry(Options()));
        Assert.Equal(1, MiddlewareCount(foundry));
    }

    // WorkflowFoundry is internal to the core assembly; MiddlewareCount is the only way to
    // observe that DisableOpenTelemetry actually unregisters its middleware instead of just
    // disposing the underlying service.
    private static int MiddlewareCount(IWorkflowFoundry foundry)
    {
        var property = foundry.GetType().GetProperty("MiddlewareCount", BindingFlags.Public | BindingFlags.Instance);
        return (int)property!.GetValue(foundry)!;
    }
}
