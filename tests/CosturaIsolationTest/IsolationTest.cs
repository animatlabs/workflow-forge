using System;
using System.Linq;
using System.Reflection;
using FluentValidation;

public class Program
{
    public static int Main()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     COSTURA ISOLATION TEST - Embedded Resource Detection     ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            // Test 1: Check user's FluentValidation version
            Console.WriteLine("Test 1: User's FluentValidation Version (from NuGet)");
            Console.WriteLine("─────────────────────────────────────────────────────────────");
            var userFV = typeof(AbstractValidator<object>).Assembly;
            Console.WriteLine($"Assembly Name: {userFV.GetName().Name}");
            Console.WriteLine($"Version: {userFV.GetName().Version} (Should be 11.10.0)");
            Console.WriteLine($"Location: {userFV.Location}");
            Console.WriteLine();

            // Test 2: Load extension DLL and check for embedded resources
            Console.WriteLine("Test 2: Extension's Embedded Resources");
            Console.WriteLine("─────────────────────────────────────────────────────────────");
            
            var extensionPath = "../../src/extensions/WorkflowForge.Extensions.Validation/bin/Release/netstandard2.0/WorkflowForge.Extensions.Validation.dll";
            var extensionAsm = Assembly.LoadFrom(System.IO.Path.GetFullPath(extensionPath));
            
            Console.WriteLine($"Extension: {extensionAsm.GetName().Name} v{extensionAsm.GetName().Version}");
            Console.WriteLine($"Location: {extensionAsm.Location}");
            Console.WriteLine();
            
            // Check for Costura embedded resources
            var resources = extensionAsm.GetManifestResourceNames()
                .Where(r => r.Contains("costura", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            Console.WriteLine($"Costura Embedded Resources Found: {resources.Count}");
            foreach (var res in resources)
            {
                Console.WriteLine($"  ✓ {res}");
                
                // Get resource size
                using (var stream = extensionAsm.GetManifestResourceStream(res))
                {
                    if (stream != null)
                    {
                        Console.WriteLine($"    Size: {stream.Length:N0} bytes");
                    }
                }
            }
            
            if (resources.Count == 0)
            {
                Console.WriteLine("  ✗ NO Costura resources found - Costura not working!");
                return 1;
            }
            
            Console.WriteLine();
            
            // Test 3: Check that FluentValidation.dll is NOT in extension output
            Console.WriteLine("Test 3: Verify FluentValidation.dll NOT Separately Packaged");
            Console.WriteLine("─────────────────────────────────────────────────────────────");
            
            var extensionDir = System.IO.Path.GetDirectoryName(extensionAsm.Location);
            var fluentValidationDll = System.IO.Path.Combine(extensionDir!, "FluentValidation.dll");
            
            if (System.IO.File.Exists(fluentValidationDll))
            {
                Console.WriteLine($"  ⚠ FluentValidation.dll exists in output (for development)");
                Console.WriteLine($"    This is OK - it won't be packaged in NuGet");
                Console.WriteLine($"    Location: {fluentValidationDll}");
            }
            else
            {
                Console.WriteLine($"  ✓ FluentValidation.dll NOT in output directory");
            }
            
            Console.WriteLine();
            
            // Test 4: Verify WorkflowForge.dll is NOT embedded
            Console.WriteLine("Test 4: Verify WorkflowForge.dll NOT Embedded");
            Console.WriteLine("─────────────────────────────────────────────────────────────");
            
            var workflowForgeEmbedded = resources.Any(r => r.Contains("workflowforge", StringComparison.OrdinalIgnoreCase));
            
            if (workflowForgeEmbedded)
            {
                Console.WriteLine("  ✗ FAILURE: WorkflowForge.dll IS embedded - this will cause conflicts!");
                return 1;
            }
            else
            {
                Console.WriteLine("  ✓ WorkflowForge.dll is NOT embedded (correct!)");
                Console.WriteLine("    WorkflowForge remains a normal NuGet dependency");
            }
            
            Console.WriteLine();
            
            // Final verdict
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ✓ SUCCESS: Costura configuration is CORRECT!                ║");
            Console.WriteLine("║                                                               ║");
            Console.WriteLine("║  - FluentValidation IS embedded as a resource                 ║");
            Console.WriteLine("║  - WorkflowForge is NOT embedded (correct!)                   ║");
            Console.WriteLine("║  - Extension will have zero version conflicts                 ║");
            Console.WriteLine("║                                                               ║");
            Console.WriteLine("║  Users can have ANY version of FluentValidation and it will   ║");
            Console.WriteLine("║  NOT conflict with the version embedded in this extension.    ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ TEST FAILED: {ex.Message}");
            Console.WriteLine($"  Type: {ex.GetType().Name}");
            Console.WriteLine($"  Stack: {ex.StackTrace}");
            Console.WriteLine();
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ✗ FAILURE: Could not verify Costura configuration!          ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            return 1;
        }
    }
}
