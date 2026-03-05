using System;
using WorkflowForge.Extensions.Resilience;

namespace WorkflowForge.Extensions.Resilience.Tests;

public class RetryExhaustedExceptionShould
{
    [Fact]
    public void SetMessageAndInnerException_GivenConstructor()
    {
        var inner = new InvalidOperationException("inner error");

        var exception = new RetryExhaustedException("All retries exhausted", inner);

        Assert.Equal("All retries exhausted", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }
}
