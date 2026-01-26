using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Persistence;
using WorkflowForge.Extensions.Persistence.Abstractions;
using WorkflowForge.Extensions.Persistence.Recovery;

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
            .AddOperation("Init", async (foundry, ct) =>
            {
                foundry.Logger.LogInformation("Init executed");
                foundry.SetProperty("seq", new List<string> { "Init" });
                await Task.Delay(25, ct);
            })
            .AddOperation("Flaky", async (foundry, ct) =>
            {
                var count = foundry.GetPropertyOrDefault("flaky.count", 0);
                foundry.SetProperty("flaky.count", count + 1);
                var seq = foundry.GetPropertyOrDefault("seq", new List<string>());
                seq.Add($"FlakyAttempt:{count + 1}");

                // Fail first 2 attempts
                if (count < 2)
                {
                    throw new InvalidOperationException($"Flaky failed on attempt {count + 1}");
                }
                foundry.Logger.LogInformation("Flaky finally succeeded");
            })
            .AddOperation("Finalize", async (foundry, ct) =>
            {
                var seq = foundry.GetPropertyOrDefault("seq", new List<string>());
                seq.Add("Finalize");
                foundry.SetProperty("done", true);
                foundry.Logger.LogInformation("Finalize executed");
            })
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
                new RecoveryPolicy { MaxAttempts = 5, BaseDelay = TimeSpan.FromMilliseconds(50) });

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


