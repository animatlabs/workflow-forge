using System;
using WorkflowForge.Extensions.Resilience;
using WorkflowForge.Extensions.Resilience.Abstractions;
using Xunit;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class RetryExhaustedExceptionShould
{
    [Fact]
    public void SetMessage_GivenConstructorWithMessageAndInnerException()
    {
        var inner = new InvalidOperationException("original failure");

        var ex = new RetryExhaustedException("All retries exhausted", inner);

        Assert.Equal("All retries exhausted", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void BeException_GivenConstructor()
    {
        var inner = new TimeoutException("timed out");

        var ex = new RetryExhaustedException("Retry exhausted after 3 attempts", inner);

        Assert.IsAssignableFrom<Exception>(ex);
    }

    [Fact]
    public void PreserveInnerExceptionType_GivenConstructorWithDifferentExceptionTypes()
    {
        var ioEx = new System.IO.IOException("IO failure");

        var ex = new RetryExhaustedException("Exhausted retrying IO operation", ioEx);

        Assert.IsType<System.IO.IOException>(ex.InnerException);
    }
}

public class CircuitBreakerOpenExceptionShould
{
    [Fact]
    public void SetMessage_GivenConstructorWithMessage()
    {
        var ex = new CircuitBreakerOpenException("Circuit breaker is open");

        Assert.Equal("Circuit breaker is open", ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void SetMessageAndInnerException_GivenConstructorWithMessageAndInnerException()
    {
        var inner = new InvalidOperationException("underlying failure");

        var ex = new CircuitBreakerOpenException("Circuit breaker tripped", inner);

        Assert.Equal("Circuit breaker tripped", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void BeException_GivenBothConstructors()
    {
        var ex1 = new CircuitBreakerOpenException("msg");
        var ex2 = new CircuitBreakerOpenException("msg", new Exception("inner"));

        Assert.IsAssignableFrom<Exception>(ex1);
        Assert.IsAssignableFrom<Exception>(ex2);
    }
}
