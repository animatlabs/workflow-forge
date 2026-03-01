using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Constants;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Tests.Extensions;

/// <summary>
/// Comprehensive tests for FoundryPropertyExtensions covering property get/set/tryGet,
/// WithOperations, WithOperation, WithMiddleware, and GetOperationOutput.
/// </summary>
public class FoundryPropertyExtensionsShould
{
    #region SetCorrelationId / GetCorrelationId

    [Fact]
    public void SetValue_GivenValidFoundry()
    {
        var foundry = CreateMockFoundry();
        const string correlationId = "corr-123";

        foundry.SetCorrelationId(correlationId);

        Assert.Equal(correlationId, foundry.GetCorrelationId());
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullFoundry()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IWorkflowFoundry)null!).SetCorrelationId("corr"));
    }

    [Fact]
    public void ReturnNull_GivenCorrelationIdNotSet()
    {
        var foundry = CreateMockFoundry();

        var result = foundry.GetCorrelationId();

        Assert.Null(result);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullFoundryForGetCorrelationId()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IWorkflowFoundry)null!).GetCorrelationId());
    }

    #endregion

    #region SetParentWorkflowExecutionId

    [Fact]
    public void SetValue_GivenValidFoundryForParentWorkflowExecutionId()
    {
        var foundry = CreateMockFoundry();
        const string parentId = "parent-exec-456";

        foundry.SetParentWorkflowExecutionId(parentId);

        Assert.True(foundry.Properties.TryGetValue(FoundryPropertyKeys.ParentWorkflowExecutionId, out var value));
        Assert.Equal(parentId, value);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullFoundryForSetParentWorkflowExecutionId()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IWorkflowFoundry)null!).SetParentWorkflowExecutionId("parent"));
    }

    #endregion

    #region TryGetProperty

    [Fact]
    public void ReturnTrueAndValue_GivenExistingMatchingType()
    {
        var foundry = CreateMockFoundry();
        foundry.SetProperty("key1", 42);

        var result = foundry.TryGetProperty<int>("key1", out var value);

        Assert.True(result);
        Assert.Equal(42, value);
    }

    [Fact]
    public void ReturnFalse_GivenExistingWrongType()
    {
        var foundry = CreateMockFoundry();
        foundry.SetProperty("key1", "string");

        var result = foundry.TryGetProperty<int>("key1", out var value);

        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void ReturnFalse_GivenNonExistentKey()
    {
        var foundry = CreateMockFoundry();

        var result = foundry.TryGetProperty<string>("nonexistent", out var value);

        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullFoundryForTryGetProperty()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IWorkflowFoundry)null!).TryGetProperty<int>("key", out _));
    }

    [Fact]
    public void ThrowArgumentException_GivenNullKey()
    {
        var foundry = CreateMockFoundry();

        Assert.Throws<ArgumentException>(() =>
            foundry.TryGetProperty<int>(null!, out _));
    }

    [Fact]
    public void ThrowArgumentException_GivenEmptyKey()
    {
        var foundry = CreateMockFoundry();

        Assert.Throws<ArgumentException>(() =>
            foundry.TryGetProperty<int>("", out _));
    }

    [Fact]
    public void ThrowArgumentException_GivenWhitespaceKey()
    {
        var foundry = CreateMockFoundry();

        Assert.Throws<ArgumentException>(() =>
            foundry.TryGetProperty<int>("   ", out _));
    }

    #endregion

    #region GetPropertyOrDefault (no default param)

    [Fact]
    public void ReturnValue_GivenExistingValue()
    {
        var foundry = CreateMockFoundry();
        foundry.SetProperty("key1", 99);

        var result = foundry.GetPropertyOrDefault<int>("key1");

        Assert.Equal(99, result);
    }

    [Fact]
    public void ReturnDefault_GivenNonExistentKey()
    {
        var foundry = CreateMockFoundry();

        var result = foundry.GetPropertyOrDefault<int>("nonexistent");

        Assert.Equal(0, result);
    }

    [Fact]
    public void ReturnDefault_GivenWrongType()
    {
        var foundry = CreateMockFoundry();
        foundry.SetProperty("key1", "string");

        var result = foundry.GetPropertyOrDefault<int>("key1");

        Assert.Equal(0, result);
    }

    #endregion

    #region GetPropertyOrDefault (with default param)

    [Fact]
    public void ReturnValue_GivenDefaultWhenValueExists()
    {
        var foundry = CreateMockFoundry();
        foundry.SetProperty("key1", 100);

        var result = foundry.GetPropertyOrDefault("key1", 999);

        Assert.Equal(100, result);
    }

    [Fact]
    public void ReturnDefault_GivenDefaultWhenValueNull()
    {
        var foundry = CreateMockFoundry();
        foundry.SetProperty("key1", (object?)null);

        var result = foundry.GetPropertyOrDefault("key1", 999);

        Assert.Equal(999, result);
    }

    [Fact]
    public void ReturnDefault_GivenDefaultWhenKeyMissing()
    {
        var foundry = CreateMockFoundry();

        var result = foundry.GetPropertyOrDefault("nonexistent", "default");

        Assert.Equal("default", result);
    }

    #endregion

    #region SetProperty

    [Fact]
    public void SetAndReturnFoundry_GivenValidKeyValue()
    {
        var foundry = CreateMockFoundry();

        var result = foundry.SetProperty("customKey", "customValue");

        Assert.Same(foundry, result);
        Assert.Equal("customValue", foundry.Properties["customKey"]);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullFoundryForSetProperty()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IWorkflowFoundry)null!).SetProperty("key", "value"));
    }

    [Fact]
    public void ThrowArgumentException_GivenNullKeyForSetProperty()
    {
        var foundry = CreateMockFoundry();

        Assert.Throws<ArgumentException>(() =>
            foundry.SetProperty(null!, "value"));
    }

    [Fact]
    public void ThrowArgumentException_GivenEmptyKeyForSetProperty()
    {
        var foundry = CreateMockFoundry();

        Assert.Throws<ArgumentException>(() =>
            foundry.SetProperty("", "value"));
    }

    [Fact]
    public void EnableMethodChaining_GivenSetProperty()
    {
        var foundry = CreateMockFoundry();

        var result = foundry
            .SetProperty("a", 1)
            .SetProperty("b", 2)
            .SetProperty("c", 3);

        Assert.Same(foundry, result);
        Assert.Equal(1, foundry.Properties["a"]);
        Assert.Equal(2, foundry.Properties["b"]);
        Assert.Equal(3, foundry.Properties["c"]);
    }

    #endregion

    #region WithOperations (params)

    [Fact]
    public void AddOperationsAndReturnFoundry_GivenParams()
    {
        var foundry = CreateMockFoundryWithAddTracking(out var addedOps, out _);
        var op1 = new Mock<IWorkflowOperation>().Object;
        var op2 = new Mock<IWorkflowOperation>().Object;

        var result = foundry.WithOperations(op1, op2);

        Assert.Same(foundry, result);
        Assert.Equal(2, addedOps.Count);
        Assert.Contains(op1, addedOps);
        Assert.Contains(op2, addedOps);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenParamsWithNullFoundry()
    {
        var op = new Mock<IWorkflowOperation>().Object;
        Assert.Throws<ArgumentNullException>(() =>
            ((IWorkflowFoundry)null!).WithOperations(op));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenParamsWithNullOperations()
    {
        var foundry = CreateMockFoundry();
        Assert.Throws<ArgumentNullException>(() =>
            foundry.WithOperations((IWorkflowOperation[])null!));
    }

    [Fact]
    public void ThrowArgumentException_GivenParamsWithNullElement()
    {
        var foundry = CreateMockFoundry();
        var op = new Mock<IWorkflowOperation>().Object;
        Assert.Throws<ArgumentException>(() =>
            foundry.WithOperations(op, null!));
    }

    #endregion

    #region WithOperations (IEnumerable)

    [Fact]
    public void AddOperationsAndReturnFoundry_GivenIEnumerable()
    {
        var foundry = CreateMockFoundryWithAddTracking(out var addedOps, out _);
        var ops = new[] { new Mock<IWorkflowOperation>().Object, new Mock<IWorkflowOperation>().Object };

        var result = foundry.WithOperations(ops.AsEnumerable());

        Assert.Same(foundry, result);
        Assert.Equal(2, addedOps.Count);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenIEnumerableWithNullFoundry()
    {
        var ops = new[] { new Mock<IWorkflowOperation>().Object };
        Assert.Throws<ArgumentNullException>(() =>
            ((IWorkflowFoundry)null!).WithOperations(ops));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenIEnumerableWithNullOperations()
    {
        var foundry = CreateMockFoundry();
        Assert.Throws<ArgumentNullException>(() =>
            foundry.WithOperations((IEnumerable<IWorkflowOperation>)null!));
    }

    #endregion

    #region WithOperation (single IWorkflowOperation)

    [Fact]
    public void AddOperationAndReturnFoundry_GivenSingle()
    {
        var foundry = CreateMockFoundryWithAddTracking(out var addedOps, out _);
        var op = new Mock<IWorkflowOperation>().Object;

        var result = foundry.WithOperation(op);

        Assert.Same(foundry, result);
        Assert.Single(addedOps);
        Assert.Same(op, addedOps[0]);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenSingleWithNullFoundry()
    {
        var op = new Mock<IWorkflowOperation>().Object;
        Assert.Throws<ArgumentNullException>(() =>
            ((IWorkflowFoundry)null!).WithOperation(op));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenSingleWithNullOperation()
    {
        var foundry = CreateMockFoundry();
        Assert.Throws<ArgumentNullException>(() =>
            foundry.WithOperation((IWorkflowOperation)null!));
    }

    #endregion

    #region WithOperation (async delegate)

    [Fact]
    public void AddDelegateOperation_GivenAsyncDelegate()
    {
        var foundry = CreateMockFoundryWithAddTracking(out var addedOps, out _);
        Func<IWorkflowFoundry, Task> action = _ => Task.CompletedTask;

        var result = foundry.WithOperation("AsyncOp", action);

        Assert.Same(foundry, result);
        Assert.Single(addedOps);
    }

    [Fact]
    public void ThrowArgumentException_GivenAsyncDelegateWithNullName()
    {
        var foundry = CreateMockFoundry();
        Func<IWorkflowFoundry, Task> action = _ => System.Threading.Tasks.Task.CompletedTask;

        Assert.Throws<ArgumentException>(() =>
            foundry.WithOperation(null!, action));
    }

    [Fact]
    public void ThrowArgumentException_GivenAsyncDelegateWithEmptyName()
    {
        var foundry = CreateMockFoundry();
        Func<IWorkflowFoundry, Task> action = _ => System.Threading.Tasks.Task.CompletedTask;

        Assert.Throws<ArgumentException>(() =>
            foundry.WithOperation("", action));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenAsyncDelegateWithNullAction()
    {
        var foundry = CreateMockFoundry();

        Assert.Throws<ArgumentNullException>(() =>
            foundry.WithOperation("Op", (Func<IWorkflowFoundry, Task>)null!));
    }

    #endregion

    #region WithOperation (sync Action)

    [Fact]
    public void AddDelegateOperation_GivenSyncAction()
    {
        var foundry = CreateMockFoundryWithAddTracking(out var addedOps, out _);
        Action<IWorkflowFoundry> action = _ => { };

        var result = foundry.WithOperation("SyncOp", action);

        Assert.Same(foundry, result);
        Assert.Single(addedOps);
    }

    [Fact]
    public void AddOperationWithRestore_GivenSyncActionWithRestoreAction()
    {
        var foundry = CreateMockFoundryWithAddTracking(out var addedOps, out _);
        Action<IWorkflowFoundry> action = _ => { };
        Action<IWorkflowFoundry> restoreAction = _ => { };

        foundry.WithOperation("Op", action, restoreAction);

        Assert.Single(addedOps);
    }

    #endregion

    #region WithMiddleware

    [Fact]
    public void AddMiddlewareAndReturnFoundry_GivenMiddleware()
    {
        var foundry = CreateMockFoundryWithAddTracking(out _, out var addedMiddleware);
        var middleware = new Mock<IWorkflowOperationMiddleware>().Object;

        var result = foundry.WithMiddleware(middleware);

        Assert.Same(foundry, result);
        Assert.Single(addedMiddleware);
        Assert.Same(middleware, addedMiddleware[0]);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullFoundryForWithMiddleware()
    {
        var middleware = new Mock<IWorkflowOperationMiddleware>().Object;
        Assert.Throws<ArgumentNullException>(() =>
            ((IWorkflowFoundry)null!).WithMiddleware(middleware));
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullMiddleware()
    {
        var foundry = CreateMockFoundry();
        Assert.Throws<ArgumentNullException>(() =>
            foundry.WithMiddleware(null!));
    }

    #endregion

    #region GetOperationOutput

    [Fact]
    public void ReturnValue_GivenExistingOutput()
    {
        var foundry = CreateMockFoundry();
        foundry.Properties["Operation.0:TestOp.Output"] = "result";

        var result = foundry.GetOperationOutput(0, "TestOp");

        Assert.Equal("result", result);
    }

    [Fact]
    public void ReturnNull_GivenNonExistentKeyForGetOperationOutput()
    {
        var foundry = CreateMockFoundry();

        var result = foundry.GetOperationOutput(0, "NonExistent");

        Assert.Null(result);
    }

    [Fact]
    public void ReturnTypedValue_GivenGenericMatchingType()
    {
        var foundry = CreateMockFoundry();
        foundry.Properties["Operation.1:IntOp.Output"] = 42;

        var result = foundry.GetOperationOutput<int>(1, "IntOp");

        Assert.Equal(42, result);
    }

    [Fact]
    public void ReturnDefault_GivenGenericWrongType()
    {
        var foundry = CreateMockFoundry();
        foundry.Properties["Operation.0:StrOp.Output"] = "hello";

        var result = foundry.GetOperationOutput<int>(0, "StrOp");

        Assert.Equal(0, result);
    }

    [Fact]
    public void ThrowArgumentNullException_GivenNullFoundryForGetOperationOutput()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IWorkflowFoundry)null!).GetOperationOutput(0, "Op"));
    }

    [Fact]
    public void ThrowArgumentOutOfRangeException_GivenNegativeIndex()
    {
        var foundry = CreateMockFoundry();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            foundry.GetOperationOutput(-1, "Op"));
    }

    [Fact]
    public void ThrowArgumentException_GivenNullOperationName()
    {
        var foundry = CreateMockFoundry();

        Assert.Throws<ArgumentException>(() =>
            foundry.GetOperationOutput(0, null!));
    }

    [Fact]
    public void ThrowArgumentException_GivenEmptyOperationName()
    {
        var foundry = CreateMockFoundry();

        Assert.Throws<ArgumentException>(() =>
            foundry.GetOperationOutput(0, ""));
    }

    #endregion

    #region Helpers

    private static IWorkflowFoundry CreateMockFoundry()
    {
        var properties = new ConcurrentDictionary<string, object?>();
        var mock = new Mock<IWorkflowFoundry>();
        mock.Setup(f => f.Properties).Returns(properties);
        mock.Setup(f => f.AddOperation(It.IsAny<IWorkflowOperation>()));
        mock.Setup(f => f.AddMiddleware(It.IsAny<IWorkflowOperationMiddleware>()));
        return mock.Object;
    }

    private static IWorkflowFoundry CreateMockFoundryWithAddTracking(
        out List<IWorkflowOperation> addedOps,
        out List<IWorkflowOperationMiddleware> addedMiddleware)
    {
        var properties = new ConcurrentDictionary<string, object?>();
        var ops = new List<IWorkflowOperation>();
        var middleware = new List<IWorkflowOperationMiddleware>();
        addedOps = ops;
        addedMiddleware = middleware;

        var mock = new Mock<IWorkflowFoundry>();
        mock.Setup(f => f.Properties).Returns(properties);
        mock.Setup(f => f.AddOperation(It.IsAny<IWorkflowOperation>()))
            .Callback<IWorkflowOperation>(op => ops.Add(op));
        mock.Setup(f => f.AddMiddleware(It.IsAny<IWorkflowOperationMiddleware>()))
            .Callback<IWorkflowOperationMiddleware>(m => middleware.Add(m));

        return mock.Object;
    }

    #endregion
}
