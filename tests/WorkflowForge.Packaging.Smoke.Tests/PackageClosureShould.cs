using System;
#if !NET48
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
// Not available in-box on .NET Framework; this test only runs on net8.0/net10.0.
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
#endif

namespace WorkflowForge.Packaging.Smoke.Tests;

#if NET48
public class PackageClosureShould
{
    [Fact(Skip = "Closure verification runs on net8.0/net10.0 (build-linux); net48 is listed only for the solution net48 test graph.")]
    public void SkippedOnNetFramework()
    {
    }
}
#else
/// <summary>
/// Verifies that every assembly a packed library references externally is either merged into it by
/// ILRepack or declared as a NuGet dependency for that target framework, so a consumer restoring
/// the package cannot hit a missing assembly at runtime.
/// </summary>
[Collection(PackagingCollection.Name)]
public class PackageClosureShould
{
    /// <summary>Assemblies supplied by the platform, which never need a NuGet dependency.</summary>
    private static readonly string[] PlatformAssemblyPrefixes =
    {
        "System",
        "mscorlib",
        "netstandard",
        "Microsoft.CSharp",
        "Microsoft.VisualBasic",
        "Microsoft.Win32",
        "WindowsBase",
    };

    [Fact]
    public async Task DeclareOrMergeEveryExternalReference_GivenPackedPackages()
    {
        var repoRoot = FindRepoRoot();
        var outputDirectory = Path.Combine(
            Path.GetTempPath(),
            "WorkflowForge.PackageClosure",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        try
        {
            // --no-build reuses the Release output this test run was built from: ILRepack runs
            // after Build, and rebuilding here would race the test host over obj/ and bin/.
            await RunDotnetAsync(
                repoRoot,
                $"pack \"{Path.Combine(repoRoot, "WorkflowForge.sln")}\" -c Release --no-build -o \"{outputDirectory}\"");

            var packages = Directory.GetFiles(outputDirectory, "*.nupkg");
            Assert.NotEmpty(packages);

            // A reference may be satisfied through another WorkflowForge package's own
            // dependencies, which NuGet resolves; third-party graphs are not available here, so
            // those must be declared directly.
            var ownPackages = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var package in packages)
            {
                using var archive = ZipFile.OpenRead(package);
                var nuspec = archive.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
                var id = Path.GetFileNameWithoutExtension(nuspec.FullName);
                ownPackages[id] = ReadDeclaredDependencies(nuspec)
                    .Values
                    .SelectMany(ids => ids)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            var violations = new List<string>();
            foreach (var package in packages)
            {
                violations.AddRange(FindUndeclaredReferences(package, ownPackages));
                violations.AddRange(FindNoticeViolations(package));
            }

            Assert.True(
                violations.Count == 0,
                "Packed assemblies reference assemblies that are neither merged in nor declared as "
                    + "NuGet dependencies:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, violations));
        }
        finally
        {
            TryDeleteDirectory(outputDirectory);
        }
    }

    private static IEnumerable<string> FindUndeclaredReferences(
        string packagePath,
        IReadOnlyDictionary<string, HashSet<string>> ownPackages)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var packageId = Path.GetFileName(packagePath);

        var nuspecEntry = archive.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        var declared = ReadDeclaredDependencies(nuspecEntry);

        foreach (var entry in archive.Entries)
        {
            var path = entry.FullName.Replace('\\', '/');
            if (!path.StartsWith("lib/", StringComparison.Ordinal) || !path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var targetFramework = path.Split('/')[1];
            var frameworkDependencies = declared.TryGetValue(targetFramework, out var forFramework)
                ? forFramework
                : declared.Values.SelectMany(v => v).ToHashSet(StringComparer.OrdinalIgnoreCase);

            frameworkDependencies = ExpandThroughOwnPackages(frameworkDependencies, ownPackages);

            foreach (var reference in ReadAssemblyReferences(entry))
            {
                if (IsPlatformAssembly(reference) || frameworkDependencies.Contains(reference))
                {
                    continue;
                }

                yield return $"  {packageId} :: {path} references '{reference}', "
                    + $"which is not merged in and not declared for {targetFramework}. "
                    + $"Declared: [{string.Join(", ", frameworkDependencies.OrderBy(d => d, StringComparer.Ordinal))}]";
            }
        }
    }

    /// <summary>
    /// Packages that ILRepack-merge a third-party library, and the libraries each one merges.
    /// Merged libraries are redistributed in binary form, so their licence notices must ship too.
    /// </summary>
    private static readonly Dictionary<string, string[]> MergedLibrariesByPackage = new(StringComparer.OrdinalIgnoreCase)
    {
        ["WorkflowForge.Extensions.Resilience.Polly"] = new[] { "Polly", "Polly.Core" },
        ["WorkflowForge.Extensions.Logging.Serilog"] = new[] { "Serilog", "Serilog.Sinks.Console" },
    };

    /// <summary>
    /// Asserts that a package carries a third-party notice when — and only when — it merges
    /// something, and that the notice names every library actually merged into it.
    /// </summary>
    private static IEnumerable<string> FindNoticeViolations(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var nuspecEntry = archive.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        var packageId = Path.GetFileNameWithoutExtension(nuspecEntry.FullName);

        var noticeEntry = archive.Entries.FirstOrDefault(
            e => e.FullName.EndsWith("THIRD-PARTY-NOTICES.txt", StringComparison.OrdinalIgnoreCase));

        if (!MergedLibrariesByPackage.TryGetValue(packageId, out var mergedLibraries))
        {
            if (noticeEntry != null)
            {
                yield return $"  {packageId} ships THIRD-PARTY-NOTICES.txt but merges nothing.";
            }

            yield break;
        }

        if (noticeEntry == null)
        {
            yield return $"  {packageId} merges {string.Join(", ", mergedLibraries)} but ships no THIRD-PARTY-NOTICES.txt.";
            yield break;
        }

        using var reader = new StreamReader(noticeEntry.Open());
        var notice = reader.ReadToEnd();
        foreach (var violation in mergedLibraries
            .Where(library => !notice.Contains(library, StringComparison.Ordinal))
            .Select(library => $"  {packageId} merges '{library}' but its THIRD-PARTY-NOTICES.txt does not mention it."))
        {
            yield return violation;
        }
    }

    /// <summary>
    /// Walks the declared set through the other packages in this pack output, so a reference
    /// satisfied by a sibling package's own dependencies counts as resolved. Only WorkflowForge
    /// packages are followed — third-party graphs are not available offline, so a reference to a
    /// third-party assembly must be declared directly.
    /// </summary>
    private static HashSet<string> ExpandThroughOwnPackages(
        HashSet<string> declared,
        IReadOnlyDictionary<string, HashSet<string>> ownPackages)
    {
        var resolved = new HashSet<string>(declared, StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<string>(declared);

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            if (!ownPackages.TryGetValue(current, out var transitive))
            {
                continue;
            }

            foreach (var id in transitive)
            {
                if (resolved.Add(id))
                {
                    pending.Enqueue(id);
                }
            }
        }

        return resolved;
    }

    /// <summary>
    /// Maps each nuspec dependency group's target framework to the package ids it declares.
    /// </summary>
    private static Dictionary<string, HashSet<string>> ReadDeclaredDependencies(ZipArchiveEntry nuspecEntry)
    {
        using var stream = nuspecEntry.Open();
        var document = XDocument.Load(stream);
        XNamespace ns = document.Root?.Name.Namespace ?? XNamespace.None;

        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in document.Descendants(ns + "group"))
        {
            var framework = NormalizeFramework((string?)group.Attribute("targetFramework") ?? string.Empty);
            if (!result.TryGetValue(framework, out var ids))
            {
                ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                result[framework] = ids;
            }

            foreach (var id in group.Elements(ns + "dependency").Select(d => (string?)d.Attribute("id")))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    ids.Add(id!);
                }
            }
        }

        return result;
    }

    /// <summary>Maps a nuspec group moniker (".NETStandard2.0") to a lib folder name ("netstandard2.0").</summary>
    private static string NormalizeFramework(string moniker) => moniker switch
    {
        ".NETStandard2.0" => "netstandard2.0",
        ".NETStandard2.1" => "netstandard2.1",
        ".NETFramework4.8" => "net48",
        _ => moniker,
    };

    private static IEnumerable<string> ReadAssemblyReferences(ZipArchiveEntry entry)
    {
        using var entryStream = entry.Open();
        using var buffer = new MemoryStream();
        entryStream.CopyTo(buffer);
        buffer.Position = 0;

        using var peReader = new PEReader(buffer);
        if (!peReader.HasMetadata)
        {
            yield break;
        }

        var metadata = peReader.GetMetadataReader();
        foreach (var handle in metadata.AssemblyReferences)
        {
            yield return metadata.GetString(metadata.GetAssemblyReference(handle).Name);
        }
    }

    private static bool IsPlatformAssembly(string name) =>
        PlatformAssemblyPrefixes.Any(prefix =>
            name.Equals(prefix, StringComparison.Ordinal)
            || name.StartsWith(prefix + ".", StringComparison.Ordinal));

    private static async Task RunDotnetAsync(string workingDirectory, string arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start dotnet process.");

        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        var standardError = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                new StringBuilder()
                    .AppendLine($"dotnet {arguments} failed with exit code {process.ExitCode}.")
                    .AppendLine(standardOutput)
                    .AppendLine(standardError)
                    .ToString());
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WorkflowForge.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate WorkflowForge.sln from the test base directory.");
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort: a stray temp directory must not fail the run.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
#endif
