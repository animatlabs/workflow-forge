using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Persistence;
using WorkflowForge.Extensions.Persistence.Recovery;
using WorkflowForge.Extensions.Persistence.Recovery.Options;

namespace WorkflowForge.Samples.BasicConsole.Samples;

public class RecoveryOnlySample : ISample
{
    public string Name => "Recovery Only";
    public string Description => "Demonstrates resume and retry without re-running completed steps";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating recovery-only flow: resume and retry until success.\n");

        var workflow = WorkflowForge.CreateWorkflow()
            .WithName("RecoveryOnlyDemo")
            .AddOperation(new InitOperation())
            .AddOperation(new FlakyOperation())
            .AddOperation(new FinalizeOperation())
            .Build();

        var checkpointsDir = Path.Combine(AppContext.BaseDirectory, "checkpoints");
        var provider = new FilePersistenceProvider(checkpointsDir);
        var options = new PersistenceOptions { InstanceId = "recovery-only", WorkflowKey = "recovery-demo" };
        var foundryKey = DeterministicGuid(options.InstanceId!);
        var workflowKey = DeterministicGuid(options.WorkflowKey!);

        // First run: will fail at Flaky; persistence keeps snapshot
        using (var f1 = WorkflowForge.CreateFoundry("RecoveryOnlyDemo"))
        {
            f1.UsePersistence(provider, options);
            var smith = WorkflowForge.CreateSmith();
            try
            {
                await smith.ForgeAsync(workflow, f1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Expected failure: {ex.Message}");
            }
        }

        // Recovery run: use ForgeWithRecoveryAsync to resume and retry until success
        using (var f2 = WorkflowForge.CreateFoundry("RecoveryOnlyDemo"))
        {
            f2.UsePersistence(provider, options);
            var smith = WorkflowForge.CreateSmith();
            await smith.ForgeWithRecoveryAsync(
                workflow,
                f2,
                provider,
                foundryKey,
                workflowKey,
                new RecoveryMiddlewareOptions { MaxRetryAttempts = 5, BaseDelay = TimeSpan.FromMilliseconds(50) });

            var seq = f2.GetPropertyOrDefault<List<string>>("seq") ?? new();
            Console.WriteLine($"Sequence: {string.Join(" -> ", seq)}");
            Console.WriteLine($"Done: {f2.GetPropertyOrDefault<bool>("done")}");
        }
    }

    private static Guid DeterministicGuid(string input)
    {
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha1.ComputeHash(bytes);
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }

    private sealed class InitOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "Init";
        public bool SupportsRestore => false;

        public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            foundry.Logger.LogInformation("Init executed");
            foundry.SetProperty("seq", new List<string> { "Init" });
            await Task.Delay(25, cancellationToken);
            return inputData;
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }

    private sealed class FlakyOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "Flaky";
        public bool SupportsRestore => false;

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var count = foundry.GetPropertyOrDefault("flaky.count", 0);
            foundry.SetProperty("flaky.count", count + 1);
            var seq = foundry.GetPropertyOrDefault("seq", new List<string>());
            seq.Add($"FlakyAttempt:{count + 1}");

            if (count < 2)
            {
                throw new InvalidOperationException($"Flaky failed on attempt {count + 1}");
            }

            foundry.Logger.LogInformation("Flaky finally succeeded");
            return Task.FromResult(inputData);
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }

    private sealed class FinalizeOperation : IWorkflowOperation
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name => "Finalize";
        public bool SupportsRestore => false;

        public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
        {
            var seq = foundry.GetPropertyOrDefault("seq", new List<string>());
            seq.Add("Finalize");
            foundry.SetProperty("done", true);
            foundry.Logger.LogInformation("Finalize executed");
            return Task.FromResult(inputData);
        }

        public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Dispose()
        { }
    }
}