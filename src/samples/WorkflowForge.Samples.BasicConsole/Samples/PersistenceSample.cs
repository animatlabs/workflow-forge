using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Persistence;
using WorkflowForge.Extensions.Persistence.Recovery;
using WorkflowForge.Extensions.Persistence.Recovery.Options;

namespace WorkflowForge.Samples.BasicConsole.Samples;

public class PersistenceSample : ISample
{
    public string Name => "Persistence (BYO Storage)";
    public string Description => "Demonstrates resumable workflows via pluggable storage provider";

    public async Task RunAsync()
    {
        Console.WriteLine("Demonstrating persisted recovery: crash mid-run, then resume and complete.\n");

        var workflow = WorkflowForge.CreateWorkflow()
            .WithName("PersistenceDemo")
            .AddOperation("Step1", async (foundry, ct) =>
            {
                foundry.Logger.LogInformation("Executing Step 1");
                foundry.SetProperty("progress", 1);
                await Task.Delay(50, ct);
            })
            .AddOperation("Step2", async (foundry, ct) =>
            {
                foundry.Logger.LogInformation("Executing Step 2");
                foundry.SetProperty("progress", 2);
                await Task.Delay(50, ct);
                // Simulate multiple failures before success to demonstrate recovery retries
                var failures = foundry.GetPropertyOrDefault("__crashCount__", 0);
                if (failures < 2)
                {
                    foundry.SetProperty("__crashCount__", failures + 1);
                    throw new InvalidOperationException($"Simulated crash after Step 2 attempt #{failures + 1}");
                }
            })
            .AddOperation("Step3", async (foundry, ct) =>
            {
                foundry.Logger.LogInformation("Executing Step 3");
                foundry.SetProperty("progress", 3);
                await Task.Delay(50, ct);
            })
            .Build();

        var fileProvider = new FilePersistenceProvider(Path.Combine(AppContext.BaseDirectory, "checkpoints"));
        var options = new PersistenceOptions { InstanceId = "PersistenceDemoFoundry", WorkflowKey = "PersistenceDemo" };

        var smith = WorkflowForge.CreateSmith();

        // First run: will crash at Step2, but persistence middleware will checkpoint and keep snapshot
        using (var foundry = WorkflowForge.CreateFoundry("PersistenceDemoFoundry"))
        {
            foundry.UsePersistence(fileProvider, options);
            try
            {
                await smith.ForgeAsync(workflow, foundry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Simulated crash encountered: {ex.Message}");
            }
        }

        // Second run (new process): create a new foundry with the same stable keys and resume
        using (var foundryResume = WorkflowForge.CreateFoundry("PersistenceDemoFoundry"))
        {
            foundryResume.UsePersistence(fileProvider, options);
            var foundryKey = DeterministicGuid(options.InstanceId!);
            var workflowKey = DeterministicGuid(options.WorkflowKey!);
            await smith.ForgeWithRecoveryAsync(
                workflow,
                foundryResume,
                fileProvider,
                foundryKey,
                workflowKey,
                new RecoveryMiddlewareOptions { MaxRetryAttempts = 5, BaseDelay = TimeSpan.FromMilliseconds(50), UseExponentialBackoff = true });
            Console.WriteLine($"After resume: progress = {foundryResume.GetPropertyOrDefault<int>("progress")}");
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

// In a real app, use a shared provider (e.g., DB/Cache) so resume works across processes/hosts.