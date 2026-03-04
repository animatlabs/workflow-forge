using System;
using WorkflowForge.Extensions.Resilience.Abstractions;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class CircuitBreakerStateChangedEventArgsShould
{
    [Fact]
    public void SetAllProperties_GivenConstructor()
    {
        var args = new CircuitBreakerStateChangedEventArgs(
            CircuitBreakerState.Closed,
            CircuitBreakerState.Open,
            "Failure threshold exceeded");

        Assert.Equal(CircuitBreakerState.Closed, args.PreviousState);
        Assert.Equal(CircuitBreakerState.Open, args.CurrentState);
        Assert.Equal("Failure threshold exceeded", args.Reason);
        Assert.True(args.Timestamp <= DateTimeOffset.UtcNow);
        Assert.True(args.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void UseFallbackTimeProvider_GivenNullTimeProvider()
    {
        var args = new CircuitBreakerStateChangedEventArgs(
            CircuitBreakerState.Open,
            CircuitBreakerState.HalfOpen,
            "Recovery attempt",
            null);

        Assert.True(args.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData(CircuitBreakerState.Closed, CircuitBreakerState.Open)]
    [InlineData(CircuitBreakerState.Open, CircuitBreakerState.HalfOpen)]
    [InlineData(CircuitBreakerState.HalfOpen, CircuitBreakerState.Closed)]
    [InlineData(CircuitBreakerState.HalfOpen, CircuitBreakerState.Open)]
    public void SupportAllStateTransitions(CircuitBreakerState from, CircuitBreakerState to)
    {
        var args = new CircuitBreakerStateChangedEventArgs(from, to, "transition");

        Assert.Equal(from, args.PreviousState);
        Assert.Equal(to, args.CurrentState);
    }
}
