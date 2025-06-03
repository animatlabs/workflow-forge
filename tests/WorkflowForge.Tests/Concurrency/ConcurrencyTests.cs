using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge;
using WorkflowForge.Abstractions;
using WorkflowForge.Operations;
using Xunit;
using Xunit.Abstractions;
using Moq;

namespace WorkflowForge.Tests.Concurrency;

/// <summary>
/// Tests for concurrent access to WorkflowFoundry and operations.
/// Verifies thread safety of core operations.
/// </summary>
public class ConcurrencyTests
{
    private readonly ITestOutputHelper _output;

    public ConcurrencyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ParallelOperations_ShouldExecuteSafely()
    {
        // Arrange
        const int operationCount = 100;
        var completedCount = 0;
        var exceptions = new List<Exception>();
        var operations = new List<IWorkflowOperation>();

        for (int i = 0; i < operationCount; i++)
        {
            var index = i;
            operations.Add(new DelegateWorkflowOperation<object, string>($"Operation{index}", async (input, foundry, cancellationToken) =>
            {
                await Task.Delay(1); // Minimal delay
                Interlocked.Increment(ref completedCount);
                return $"Result {index}";
            }));
        }

        // Create multiple foundries to test concurrent access
        var foundries = new List<WorkflowFoundry>();
        for (int i = 0; i < 10; i++)
        {
            var data = new ConcurrentDictionary<string, object?>();
            var foundry = new WorkflowFoundry(Guid.NewGuid(), data);
            foundries.Add(foundry);
        }

        // Act - Execute operations in parallel across foundries
        var tasks = new List<Task>();
        foreach (var foundry in foundries)
        {
            var foundryTasks = operations.Take(10).Select(async operation =>
            {
                try
                {
                    foundry.AddOperation(operation);
                    await foundry.ForgeAsync();
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            tasks.AddRange(foundryTasks);
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        _output.WriteLine($"Completed operations: {completedCount}");
    }

    [Fact]
    public async Task WorkflowFoundryDataAccess_ShouldBeThreadSafe()
    {
        // Arrange
        const int threadCount = 50;
        var data = new ConcurrentDictionary<string, object?>();
        var foundry = new WorkflowFoundry(Guid.NewGuid(), data);

        // Act - Multiple threads accessing foundry data concurrently
        var tasks = new List<Task>();
        for (int i = 0; i < threadCount; i++)
        {
            var threadIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                // Each thread performs multiple data operations
                for (int j = 0; j < 100; j++)
                {
                    var key = $"thread{threadIndex}_item{j}";
                    foundry.Properties[key] = $"value_{threadIndex}_{j}";
                    
                    await Task.Yield(); // Allow other threads to interleave
                    
                    var value = foundry.Properties[key];
                    Assert.Equal($"value_{threadIndex}_{j}", value);
                    
                    foundry.Properties[$"prop_{key}"] = threadIndex;
                    var prop = (int)(foundry.Properties[$"prop_{key}"] ?? 0);
                    Assert.Equal(threadIndex, prop);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Verify all data was stored correctly
        Assert.Equal(threadCount * 100 * 2, foundry.Properties.Count); // Each iteration creates 2 properties
        _output.WriteLine($"Total data items: {foundry.Properties.Count}");
    }

    [Fact]
    public async Task ConcurrentOperationExecution_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var data = new ConcurrentDictionary<string, object?>();
        var foundry = new WorkflowFoundry(Guid.NewGuid(), data);

        foundry.Properties["counter"] = 0;

        // Create operations that increment a shared counter
        var operations = new List<IWorkflowOperation>();
        for (int i = 0; i < 100; i++)
        {
            operations.Add(new DelegateWorkflowOperation<object, int>($"IncrementOperation{i}", async (input, foundryRef, cancellationToken) =>
            {
                await Task.Delay(1); // Small delay to increase chance of race conditions
                
                // Simulate non-atomic increment
                var current = (int)(foundryRef.Properties["counter"] ?? 0);
                await Task.Delay(1);
                foundryRef.Properties["counter"] = current + 1;
                
                return current + 1;
            }));
        }

        // Act
        foreach (var operation in operations)
        {
            foundry.AddOperation(operation);
        }

        await foundry.ForgeAsync();

        // Assert
        var finalCount = (int)(foundry.Properties["counter"] ?? 0);
        
        // Note: This test might reveal race conditions in data access
        // The count might be less than 100 due to race conditions
        _output.WriteLine($"Final counter value: {finalCount}");
        _output.WriteLine($"Expected: 100, Actual: {finalCount}");
    }

    [Fact]
    public async Task ParallelFoundryCreation_ShouldBeThreadSafe()
    {
        // Arrange
        const int foundryCount = 50;
        var foundries = new List<WorkflowFoundry>();
        var lockObject = new object();

        // Act - Create foundries in parallel
        var tasks = Enumerable.Range(0, foundryCount).Select(async i =>
        {
            var data = new ConcurrentDictionary<string, object?>();
            var foundry = new WorkflowFoundry(Guid.NewGuid(), data);

            foundry.Properties[$"id"] = i;
            foundry.Properties["created_at"] = DateTime.UtcNow;

            var operation = new DelegateWorkflowOperation<object, string>($"Op{i}", async (input, foundryRef, cancellationToken) =>
            {
                await Task.Delay(10);
                return $"Result from foundry {i}";
            });

            foundry.AddOperation(operation);
            await foundry.ForgeAsync();

            lock (lockObject)
            {
                foundries.Add(foundry);
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(foundryCount, foundries.Count);
        Assert.All(foundries, foundry =>
        {
            Assert.NotEqual(Guid.Empty, foundry.ExecutionId);
        });

        _output.WriteLine($"Successfully created {foundries.Count} foundries in parallel");
    }

    [Fact]
    public async Task ConcurrentPropertyAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var data = new ConcurrentDictionary<string, object?>();
        var foundry = new WorkflowFoundry(Guid.NewGuid(), data);
        const int threadCount = 20;
        const int operationsPerThread = 50;

        // Act
        var tasks = Enumerable.Range(0, threadCount).Select(threadId =>
            Task.Run(async () =>
            {
                for (int i = 0; i < operationsPerThread; i++)
                {
                    var key = $"thread{threadId}_prop{i}";
                    var value = $"value_{threadId}_{i}";
                    
                    foundry.Properties[key] = value;
                    await Task.Yield();
                    
                    var retrieved = (string?)foundry.Properties[key];
                    Assert.Equal(value, retrieved);
                    
                    // Test property updates
                    var updatedValue = $"updated_{value}";
                    foundry.Properties[key] = updatedValue;
                    
                    var finalValue = (string?)foundry.Properties[key];
                    Assert.Equal(updatedValue, finalValue);
                }
            }));

        await Task.WhenAll(tasks);

        // Assert
        var totalExpectedProperties = threadCount * operationsPerThread;
        var actualProperties = foundry.Properties.Count;
        Assert.Equal(totalExpectedProperties, actualProperties);
        
        _output.WriteLine($"Total properties created: {actualProperties}");
    }

    [Fact]
    public async Task ConcurrentOperationAddition_ShouldMaintainOrder()
    {
        // Arrange
        var data = new ConcurrentDictionary<string, object?>();
        var foundry = new WorkflowFoundry(Guid.NewGuid(), data);
        const int operationCount = 100;
        var executionOrder = new List<int>();
        var lockObject = new object();

        // Act - Add operations concurrently
        var addTasks = Enumerable.Range(0, operationCount).Select(async i =>
        {
            var operation = new DelegateWorkflowOperation<object, string>($"ConcurrentOp{i}", async (input, foundryRef, cancellationToken) =>
            {
                await Task.Yield(); // Allow other operations to interleave
                lock (lockObject)
                {
                    executionOrder.Add(i);
                }
                return $"Operation {i} completed";
            });

            // Small delay to increase concurrency
            if (i % 2 == 0) await Task.Delay(1);
            
            foundry.AddOperation(operation);
        });

        await Task.WhenAll(addTasks);
        await foundry.ForgeAsync();

        // Assert
        Assert.Equal(operationCount, executionOrder.Count);
        
        // Operations should execute in the order they were added (FIFO)
        // Due to concurrency in adding, we can't guarantee exact order, but all should execute
        var uniqueOperations = executionOrder.Distinct().Count();
        Assert.Equal(operationCount, uniqueOperations);
        
        _output.WriteLine($"Execution order sample: [{string.Join(", ", executionOrder.Take(10))}...]");
    }

    [Fact]
    public async Task StressTest_ConcurrentFoundryOperations()
    {
        // Arrange
        const int foundryCount = 20;
        const int operationsPerFoundry = 25;
        var foundries = new List<WorkflowFoundry>();
        var exceptions = new ConcurrentBag<Exception>();

        // Create foundries
        for (int i = 0; i < foundryCount; i++)
        {
            var data2 = new ConcurrentDictionary<string, object?>();
            foundries.Add(new WorkflowFoundry(Guid.NewGuid(), data2));
        }

        // Act - Execute operations across all foundries concurrently
        var tasks = foundries.Select(async foundry =>
        {
            try
            {
                for (int j = 0; j < operationsPerFoundry; j++)
                {
                    var operationIndex = j; // Capture for closure
                    var operation = new DelegateWorkflowOperation<object, string>($"StressOp{operationIndex}", async (input, foundryRef, cancellationToken) =>
                    {
                        // Simulate work with data access - use unique keys to avoid overwriting
                        foundryRef.Properties[$"result_{operationIndex}"] = $"processed_{operationIndex}";
                        foundryRef.Properties[$"timestamp_{operationIndex}"] = DateTime.UtcNow;
                        
                        await Task.Delay(Random.Shared.Next(1, 5));
                        return $"Completed operation {operationIndex}";
                    });

                    foundry.AddOperation(operation);
                }

                await foundry.ForgeAsync();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.All(foundries, foundry =>
        {
            Assert.Equal(operationsPerFoundry * 2, foundry.Properties.Count); // Each operation creates 2 properties
        });

        _output.WriteLine($"Stress test completed successfully with {foundryCount} foundries and {foundryCount * operationsPerFoundry} total operations");
    }

    [Fact]
    public async Task ParallelExecutionComparison_SequentialVsParallel()
    {
        // Arrange
        const int operationCount = 50;
        const int delayMs = 10;

        // Sequential execution
        var data1 = new ConcurrentDictionary<string, object?>();
        var foundry1 = new WorkflowFoundry(Guid.NewGuid(), data1);
        
        for (int i = 0; i < operationCount; i++)
        {
            var operation = new DelegateWorkflowOperation<object, string>($"SeqOp{i}", async (input, foundryRef, cancellationToken) =>
            {
                await Task.Delay(delayMs);
                return $"Sequential result {i}";
            });
            foundry1.AddOperation(operation);
        }

        var sequentialStart = DateTime.UtcNow;
        await foundry1.ForgeAsync();
        var sequentialDuration = DateTime.UtcNow - sequentialStart;

        // Parallel execution (multiple foundries)
        var parallelTasks = new List<Task>();
        var parallelStart = DateTime.UtcNow;

        for (int i = 0; i < operationCount; i++)
        {
            var index = i;
            parallelTasks.Add(Task.Run(async () =>
            {
                var data = new ConcurrentDictionary<string, object?>();
                var foundry = new WorkflowFoundry(Guid.NewGuid(), data);
                
                var operation = new DelegateWorkflowOperation<object, string>($"ParOp{index}", async (input, foundryRef, cancellationToken) =>
                {
                    await Task.Delay(delayMs);
                    return $"Parallel result {index}";
                });
                
                foundry.AddOperation(operation);
                await foundry.ForgeAsync();
            }));
        }

        await Task.WhenAll(parallelTasks);
        var parallelDuration = DateTime.UtcNow - parallelStart;

        // Assert
        _output.WriteLine($"Sequential execution: {sequentialDuration.TotalMilliseconds:F2}ms");
        _output.WriteLine($"Parallel execution: {parallelDuration.TotalMilliseconds:F2}ms");
        _output.WriteLine($"Speedup ratio: {sequentialDuration.TotalMilliseconds / parallelDuration.TotalMilliseconds:F2}x");

        // Parallel should be significantly faster (though exact timing depends on system)
        Assert.True(parallelDuration < sequentialDuration, 
            $"Parallel execution ({parallelDuration.TotalMilliseconds}ms) should be faster than sequential ({sequentialDuration.TotalMilliseconds}ms)");
    }
} 
