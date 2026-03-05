using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Testing;

namespace WorkflowForge.Tests.ExtensionsTests;

public class WorkflowForgeLoggerExtensionsShould : IDisposable
{
    private readonly IWorkflowForgeLogger _logger;
    private readonly FakeWorkflowFoundry _foundry;

    public WorkflowForgeLoggerExtensionsShould()
    {
        _logger = TestNullLogger.Instance;
        _foundry = new FakeWorkflowFoundry();
    }

    public void Dispose()
    {
        _foundry.Dispose();
    }

    [Fact]
    public void ReturnDisposable_GivenCreateWorkflowScope()
    {
        var workflow = new Mock<IWorkflow>();
        workflow.SetupGet(w => w.Id).Returns(Guid.NewGuid());
        workflow.SetupGet(w => w.Name).Returns("TestWorkflow");
        workflow.SetupGet(w => w.Operations).Returns(new List<IWorkflowOperation>());

        using var scope = _logger.CreateWorkflowScope(workflow.Object, _foundry);

        Assert.NotNull(scope);
    }

    [Fact]
    public void IncludeParentWorkflowId_GivenCreateWorkflowScopeWithParentId()
    {
        var parentId = Guid.NewGuid();
        _foundry.Properties["ParentWorkflowExecutionId"] = parentId;

        var workflow = new Mock<IWorkflow>();
        workflow.SetupGet(w => w.Id).Returns(Guid.NewGuid());
        workflow.SetupGet(w => w.Name).Returns("ChildWorkflow");
        workflow.SetupGet(w => w.Operations).Returns(new List<IWorkflowOperation>());

        using var scope = _logger.CreateWorkflowScope(workflow.Object, _foundry);

        Assert.NotNull(scope);
    }

    [Fact]
    public void HandleNullParentId_GivenCreateWorkflowScopeWithNullParentIdValue()
    {
        _foundry.Properties["ParentWorkflowExecutionId"] = null;

        var workflow = new Mock<IWorkflow>();
        workflow.SetupGet(w => w.Id).Returns(Guid.NewGuid());
        workflow.SetupGet(w => w.Name).Returns("ChildWorkflow");
        workflow.SetupGet(w => w.Operations).Returns(new List<IWorkflowOperation>());

        using var scope = _logger.CreateWorkflowScope(workflow.Object, _foundry);

        Assert.NotNull(scope);
    }

    [Fact]
    public void ReturnDisposable_GivenCreateOperationScope()
    {
        var operation = new Mock<IWorkflowOperation>();
        operation.SetupGet(o => o.Id).Returns(Guid.NewGuid());
        operation.SetupGet(o => o.Name).Returns("TestOp");

        using var scope = _logger.CreateOperationScope(operation.Object, 0);

        Assert.NotNull(scope);
    }

    [Fact]
    public void ReturnDisposable_GivenCreateOperationScopeWithInputData()
    {
        var operation = new Mock<IWorkflowOperation>();
        operation.SetupGet(o => o.Id).Returns(Guid.NewGuid());
        operation.SetupGet(o => o.Name).Returns("TestOp");

        using var scope = _logger.CreateOperationScope(operation.Object, 2, "input-data");

        Assert.NotNull(scope);
    }

    [Fact]
    public void ReturnDisposable_GivenCreateCompensationScope()
    {
        using var scope = _logger.CreateCompensationScope(5);

        Assert.NotNull(scope);
    }

    [Fact]
    public void ReturnProperties_GivenCreateErrorProperties()
    {
        var ex = new InvalidOperationException("test");

        var props = _logger.CreateErrorProperties(ex);

        Assert.Equal("InvalidOperationException", props["ExceptionType"]);
        Assert.Equal("UnhandledException", props["ErrorCategory"]);
        Assert.NotNull(props["ErrorCode"]);
    }

    [Fact]
    public void UseCustomCategory_GivenCreateErrorPropertiesWithCategory()
    {
        var ex = new ArgumentException("arg");

        var props = _logger.CreateErrorProperties(ex, "ValidationError");

        Assert.Equal("ArgumentException", props["ExceptionType"]);
        Assert.Equal("ValidationError", props["ErrorCategory"]);
    }

    [Fact]
    public void ReturnProperties_GivenCreateCompensationResultProperties()
    {
        var props = _logger.CreateCompensationResultProperties(3, 1);

        Assert.Equal("3", props["CompensationSuccessCount"]);
        Assert.Equal("1", props["CompensationFailureCount"]);
    }
}
