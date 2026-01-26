using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Persistence;
using WorkflowForge.Extensions.Persistence.Recovery;
using WorkflowForge.Extensions.Resilience;

namespace WorkflowForge.Samples.BasicConsole.Samples;

public class ResilienceRecoverySample : ISample
{
    public string Name => "Recovery + Resilience";
    public string Description => "Shows using base Resilience retry with Recovery resume for unified behavior";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating Recovery + Resilience working together.\n");

        // Workflow with a flaky step
        var workflow = WorkflowForge.CreateWorkflow()
            .WithName("ResilienceRecoveryDemo")
            .AddOperation("Init", async (foundry, ct) =>
            {
                if (!foundry.TryGetProperty<DateTimeOffset>("startedAt", out _))
                {
                    foundry.SetProperty("startedAt", DateTimeOffset.UtcNow);
                }
                foundry.SetProperty("seq", new List<string> { "Init" });
                await Task.Delay(10, ct);
            })
            .AddOperation("Flaky", async (foundry, ct) =>
            {
                var seq = foundry.GetPropertyOrDefault("seq", new List<string>());
                seq.Add("FlakyAttempt");
                var startedAt = foundry.GetPropertyOrDefault("startedAt", DateTimeOffset.UtcNow);
                var elapsed = DateTimeOffset.UtcNow - startedAt;
                // Simulate transient condition that resolves after ~50ms total elapsed time
                if (elapsed < TimeSpan.FromMilliseconds(50))
                {
                    throw new InvalidOperationException($"Flaky failed due to transient condition (elapsed {elapsed.TotalMilliseconds:F0}ms)");
                }
            })
            .AddOperation("Finalize", async (foundry, ct) =>
            {
                var seq = foundry.GetPropertyOrDefault("seq", new List<string>());
                seq.Add("Finalize");
                foundry.SetProperty("done", true);
            })
            .Build();

        var checkpoints = Path.Combine(AppContext.BaseDirectory, "checkpoints");
        var provider = new FilePersistenceProvider(checkpoints);
        var options = new PersistenceOptions { InstanceId = "res-rec-instance", WorkflowKey = "res-rec-demo" };
        var foundryKey = DeterministicGuid(options.InstanceId!);
        var workflowKey = DeterministicGuid(options.WorkflowKey!);

        // Ensure clean start for deterministic behavior
        var checkpointFile = Path.Combine(checkpoints, $"{foundryKey:N}_{workflowKey:N}.json");
        if (File.Exists(checkpointFile)) File.Delete(checkpointFile);

        // First run: enable persistence + a simple retry middleware for transient errors
        using (var f1 = WorkflowForge.CreateFoundry("ResilienceRecoveryDemo"))
        {
            f1.UsePersistence(provider, options);
            // Add a base Resilience retry middleware (e.g., exponential backoff 2 attempts)
            var retry = RetryMiddleware.WithExponentialBackoff(
                f1.Logger,
                initialDelay: TimeSpan.FromMilliseconds(25),
                maxDelay: TimeSpan.FromMilliseconds(75),
                maxAttempts: 2);
            f1.AddMiddleware(retry);

            var smith = WorkflowForge.CreateSmith();
            try
            {
                await smith.ForgeAsync(workflow, f1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Expected failure after retries: {ex.GetType().Name}: {ex.Message}");
            }
            Console.WriteLine($"Checkpoint created: {File.Exists(checkpointFile)} at {checkpointFile}");
        }

        // Recovery run: resume from checkpoint and allow a few more retries if needed
        using (var f2 = WorkflowForge.CreateFoundry("ResilienceRecoveryDemo"))
        {
            f2.UsePersistence(provider, options);
            // Same retry middleware for unified experience
            var retry = RetryMiddleware.WithExponentialBackoff(
                f2.Logger,
                initialDelay: TimeSpan.FromMilliseconds(100),
                maxDelay: TimeSpan.FromMilliseconds(400),
                maxAttempts: 2);
            f2.AddMiddleware(retry);

            var smith = WorkflowForge.CreateSmith();
            Console.WriteLine("Starting recovery phase...");
            await smith.ForgeWithRecoveryAsync(
                workflow,
                f2,
                provider,
                foundryKey,
                workflowKey,
                new RecoveryPolicy { MaxAttempts = 3, BaseDelay = TimeSpan.FromMilliseconds(50), UseExponentialBackoff = true });

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
}


