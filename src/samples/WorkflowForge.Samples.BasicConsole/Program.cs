using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkflowForge;
using WorkflowForge.Extensions.Resilience.Polly.Configurations;
using WorkflowForge.Extensions.Observability.Performance;
using WorkflowForge.Samples.BasicConsole.Samples;

namespace WorkflowForge.Samples.BasicConsole;

/// <summary>
/// WorkflowForge Samples Console Application
/// 
/// Simple interactive menu to run WorkflowForge examples and demonstrations.
/// </summary>
public static class Program
{
    public static IConfiguration Configuration { get; private set; } = null!;
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    private static readonly Dictionary<string, ISample> Samples = new()
    {
        // Basic Samples (1-4)
        ["1"] = new HelloWorldSample(),
        ["2"] = new DataPassingSample(),
        ["3"] = new MultipleOutcomesSample(),
        ["4"] = new InlineOperationsSample(),
        
        // Control Flow Samples (5-8)
        ["5"] = new ConditionalWorkflowSample(),
        ["6"] = new ForEachLoopSample(),
        ["7"] = new ErrorHandlingSample(),
        ["8"] = new BuiltInOperationsSample(),
        
        // Configuration & Middleware Samples (9-12)
        ["9"] = new OptionsPatternSample(),
        ["10"] = new ConfigurationProfilesSample(),
        ["11"] = new WorkflowEventsSample(),
        ["12"] = new MiddlewareSample(),
        
        // Extension Samples (13-17)
        ["13"] = new SerilogIntegrationSample(),
        ["14"] = new PollyResilienceSample(),
        ["15"] = new OpenTelemetryObservabilitySample(),
        ["16"] = new HealthChecksSample(),
        ["17"] = new PerformanceMonitoringSample(),
        
        // Advanced Samples (18-19)
        ["18"] = new ComprehensiveIntegrationSample(),
        ["19"] = new OperationCreationPatternsSample(),
    };

    public static async Task Main(string[] args)
    {
        // Setup configuration
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables();
        
        Configuration = configurationBuilder.Build();

        // Setup services
        var services = new ServiceCollection();
        services.AddSingleton(Configuration);
        services.Configure<WorkflowForgeSettings>(Configuration.GetSection(WorkflowForgeSettings.SectionName));
        services.Configure<PollySettings>(Configuration.GetSection("WorkflowForge:Polly"));
        services.Configure<PerformanceSettings>(Configuration.GetSection("WorkflowForge:Performance"));
        services.AddLogging(builder => 
        {
            builder.AddConfiguration(Configuration.GetSection("Logging"));
            builder.AddConsole();
        });
        
        ServiceProvider = services.BuildServiceProvider();

        Console.Clear();
        PrintHeader();
        
        try
        {
            await RunInteractiveMenu();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    private static void PrintHeader()
    {
        Console.WriteLine("WorkflowForge - Sample Applications");
        Console.WriteLine("===================================");
        Console.WriteLine("Interactive examples demonstrating WorkflowForge capabilities");
        Console.WriteLine();
    }

    private static async Task RunInteractiveMenu()
    {
        bool running = true;
        
        while (running)
        {
            Console.WriteLine();
            Console.WriteLine("Available Samples:");
            Console.WriteLine("------------------");
            Console.WriteLine();
            Console.WriteLine("BASIC WORKFLOWS:");
            Console.WriteLine("  1.  Hello World              - Simple workflow demonstration");
            Console.WriteLine("  2.  Data Passing             - Pass data between operations");
            Console.WriteLine("  3.  Multiple Outcomes        - Workflows with different results");
            Console.WriteLine("  4.  Inline Operations        - Quick operations with lambdas");
            Console.WriteLine();
            Console.WriteLine("CONTROL FLOW:");
            Console.WriteLine("  5.  Conditional Workflows    - If/else logic in workflows");
            Console.WriteLine("  6.  ForEach Loops            - Process collections");
            Console.WriteLine("  7.  Error Handling           - Handle exceptions gracefully");
            Console.WriteLine("  8.  Built-in Operations      - Use logging, delays, etc.");
            Console.WriteLine();
            Console.WriteLine("CONFIGURATION & MIDDLEWARE:");
            Console.WriteLine("  9.  Options Pattern          - Configuration management");
            Console.WriteLine("  10. Configuration Profiles   - Environment-specific settings");
            Console.WriteLine("  11. Workflow Events          - Listen to workflow lifecycle");
            Console.WriteLine("  12. Middleware Usage         - Add cross-cutting concerns");
            Console.WriteLine();
            Console.WriteLine("EXTENSIONS:");
            Console.WriteLine("  13. Serilog Logging         - Structured logging");
            Console.WriteLine("  14. Polly Resilience        - Retry policies and circuit breakers");
            Console.WriteLine("  15. OpenTelemetry           - Distributed tracing");
            Console.WriteLine("  16. Health Checks           - System monitoring");
            Console.WriteLine("  17. Performance Monitoring  - Metrics and statistics");
            Console.WriteLine();
            Console.WriteLine("ADVANCED:");
            Console.WriteLine("  18. Comprehensive Demo      - Full-featured example");
            Console.WriteLine("  19. Operation Creation Patterns - All operation creation methods");
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("Quick Options:");
            Console.WriteLine("  A   - Run ALL samples (may take a while!)");
            Console.WriteLine("  B   - Run Basic samples only (1-4)");
            Console.WriteLine("  Q   - Quit");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.Write("Enter your choice: ");
            
            var input = Console.ReadLine()?.Trim().ToUpperInvariant();
            
            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("Please enter a valid choice.");
                continue;
            }

            switch (input)
            {
                case "Q":
                case "QUIT":
                case "EXIT":
                    running = false;
                    Console.WriteLine("Goodbye!");
                    break;
                
                case "A":
                case "ALL":
                    await RunAllSamples();
                    break;
                
                case "B":
                case "BASIC":
                    await RunBasicSamples();
                    break;
                
                default:
                    if (Samples.ContainsKey(input))
                    {
                        await RunSample(input, Samples[input]);
                    }
                    else
                    {
                        Console.WriteLine($"Invalid choice: {input}");
                        Console.WriteLine("Please enter a number (1-19), A for all, B for basic, or Q to quit.");
                    }
                    break;
            }

            if (running && input != "A" && input != "B")
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to return to menu...");
                Console.ReadKey();
                Console.Clear();
                PrintHeader();
            }
        }
    }

    private static async Task RunAllSamples()
    {
        Console.WriteLine();
        Console.WriteLine("Running ALL samples...");
        Console.WriteLine("======================");
        
        foreach (var kvp in Samples)
        {
            await RunSample(kvp.Key, kvp.Value);
            Console.WriteLine();
        }
        
        Console.WriteLine("All samples completed successfully.");
        Console.WriteLine();
        Console.WriteLine("Press any key to return to menu...");
        Console.ReadKey();
        Console.Clear();
        PrintHeader();
    }

    private static async Task RunBasicSamples()
    {
        Console.WriteLine();
        Console.WriteLine("Running Basic samples (1-4)...");
        Console.WriteLine("===============================");
        
        for (int i = 1; i <= 4; i++)
        {
            if (Samples.ContainsKey(i.ToString()))
            {
                await RunSample(i.ToString(), Samples[i.ToString()]);
                Console.WriteLine();
            }
        }
        
        Console.WriteLine("Basic samples completed successfully.");
        Console.WriteLine();
        Console.WriteLine("Press any key to return to menu...");
        Console.ReadKey();
        Console.Clear();
        PrintHeader();
    }

    private static async Task RunSample(string number, ISample sample)
    {
        var sampleName = sample.GetType().Name.Replace("Sample", "");
        
        Console.WriteLine();
        Console.WriteLine($"[RUNNING] Sample {number}: {sampleName}");
        Console.WriteLine($"{"".PadLeft(50, '-')}");
        
        try
        {
            await sample.RunAsync();
            Console.WriteLine($"[SUCCESS] Sample {number} completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Sample {number} failed: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"        Inner Exception: {ex.InnerException.Message}");
            }
        }
    }
} 
