using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WorkflowForge.Packaging.Smoke.Tests;

#if NET48
public class PackagingSmokeShould
{
    [Fact(Skip = "Packaging smoke runs on net8.0/net10.0 (build-linux). net48 is listed only for solution net48 test graph.")]
    public void SkippedOnNetFramework()
    {
    }
}
#else
[Collection(PackagingCollection.Name)]
public class PackagingSmokeShould
{
    [Fact]
    public async Task RunConsumer_GivenPackedCoreAndIlRepackExtensions()
    {
        var repoRoot = FindRepoRoot();
        var uniqueId = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}";
        var workRoot = Path.Combine(Path.GetTempPath(), "WorkflowForge.Packaging.Smoke", uniqueId);
        var feedDir = Path.Combine(workRoot, "feed");
        var consumerDir = Path.Combine(workRoot, "consumer");
        Directory.CreateDirectory(feedDir);
        Directory.CreateDirectory(consumerDir);

        try
        {
            var coreProject = Path.Combine(repoRoot, "src", "core", "WorkflowForge", "WorkflowForge.csproj");
            var serilogProject = Path.Combine(
                repoRoot,
                "src",
                "extensions",
                "WorkflowForge.Extensions.Logging.Serilog",
                "WorkflowForge.Extensions.Logging.Serilog.csproj");
            var resilienceProject = Path.Combine(
                repoRoot,
                "src",
                "extensions",
                "WorkflowForge.Extensions.Resilience",
                "WorkflowForge.Extensions.Resilience.csproj");
            var pollyProject = Path.Combine(
                repoRoot,
                "src",
                "extensions",
                "WorkflowForge.Extensions.Resilience.Polly",
                "WorkflowForge.Extensions.Resilience.Polly.csproj");

            foreach (var project in new[] { coreProject, serilogProject, resilienceProject, pollyProject })
            {
                await RunDotnetAsync(repoRoot, $"pack \"{project}\" -c Release --no-build -o \"{feedDir}\"");
            }

            var corePackage = Directory.GetFiles(feedDir, "WorkflowForge.*.nupkg")
                .Where(f => !Path.GetFileName(f).Contains(".Extensions.", StringComparison.Ordinal))
                .Single();

            await RunDotnetAsync(consumerDir, "new console -n Consumer -f net8.0 --force");
            var consumerProject = Path.Combine(consumerDir, "Consumer", "Consumer.csproj");

            var nugetConfig = Path.Combine(consumerDir, "Consumer", "NuGet.config");
            await File.WriteAllTextAsync(
                nugetConfig,
                $"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <packageSources>
                    <clear />
                    <add key="local" value="{feedDir}" />
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                  </packageSources>
                </configuration>
                """);

            var coreFileName = Path.GetFileName(corePackage)!;
            var coreVersion = coreFileName["WorkflowForge.".Length..^".nupkg".Length];

            var packageIds = new[]
            {
                "WorkflowForge",
                "WorkflowForge.Extensions.Logging.Serilog",
                "WorkflowForge.Extensions.Resilience.Polly",
            };

            foreach (var packageId in packageIds)
            {
                await RunDotnetAsync(
                    Path.Combine(consumerDir, "Consumer"),
                    $"add \"{consumerProject}\" package {packageId} --version {coreVersion}");
            }

            // Touching the merged types at runtime is the point: a package whose third-party
            // library was neither merged nor declared still compiles, and only fails on load.
            var programPath = Path.Combine(consumerDir, "Consumer", "Program.cs");
            await File.WriteAllTextAsync(
                programPath,
                """
                using WorkflowForge.Extensions.Logging.Serilog;
                using WorkflowForge.Extensions.Resilience.Polly;

                var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
                {
                    MinimumLevel = "Information",
                    EnableConsoleSink = false,
                });
                logger.LogInformation("serilog-ok");

                using var foundry = global::WorkflowForge.WorkflowForge.CreateFoundry("SmokeTest", logger);
                foundry.UsePollyRetry(maxRetryAttempts: 2);

                using var smith = global::WorkflowForge.WorkflowForge.CreateSmith();
                Console.WriteLine(smith != null ? "ok" : "fail");
                """);

            await RunDotnetAsync(Path.Combine(consumerDir, "Consumer"), $"build \"{consumerProject}\" -c Release");

            var output = await RunDotnetAsync(
                Path.Combine(consumerDir, "Consumer"),
                $"run --project \"{consumerProject}\" -c Release --no-build");

            Assert.Contains("ok", output, StringComparison.Ordinal);
        }
        finally
        {
            TryDeleteDirectory(workRoot);
        }
    }

    private static async Task<string> RunDotnetAsync(string workingDirectory, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet process.");

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet {arguments} failed with exit code {process.ExitCode}.{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
        }

        return stdout;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "WorkflowForge.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate WorkflowForge.sln from test base directory.");
    }

    private static void TryDeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 2)
            {
                Task.Delay(100 * (attempt + 1)).GetAwaiter().GetResult();
            }
            catch (UnauthorizedAccessException) when (attempt < 2)
            {
                Task.Delay(100 * (attempt + 1)).GetAwaiter().GetResult();
            }
        }
    }
}
#endif
