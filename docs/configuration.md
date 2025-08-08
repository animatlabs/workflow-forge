# WorkflowForge Configuration Guide

This guide covers configuration strategies for WorkflowForge across different environments and use cases.

## Configuration Principles

### 1. Environment-Specific Configuration
WorkflowForge supports different configuration profiles for different environments:
- **Development** - Verbose logging, detailed metrics, minimal resilience
- **Production** - Optimized performance, essential logging, comprehensive resilience
- **Enterprise** - Full observability, comprehensive monitoring, advanced security

### 2. Configuration Sources
WorkflowForge can be configured through multiple sources:
- **appsettings.json** - Base configuration
- **Environment variables** - Override for deployment
- **Code-based configuration** - Programmatic setup
- **External configuration** - Azure App Configuration, AWS Systems Manager, etc.

## Basic Configuration

### appsettings.json Structure

```json
{
  "WorkflowForge": {
    "Core": {
      "DefaultTimeout": "00:05:00",
      "EnableCompensation": true,
      "MaxConcurrentWorkflows": 10
    },
    "Logging": {
      "Provider": "Serilog",
      "MinimumLevel": "Information",
      "Configuration": {
        "WriteTo": [
          { "Name": "Console" },
          { 
            "Name": "File", 
            "Args": { 
              "path": "logs/workflow-.txt",
              "rollingInterval": "Day" 
            } 
          }
        ]
      }
    },
    "Resilience": {
      "Provider": "Polly",
      "Polly": {
        "Retry": {
          "MaxAttempts": 3,
          "BaseDelay": "00:00:01",
          "UseExponentialBackoff": true,
          "UseJitter": true
        },
        "CircuitBreaker": {
          "FailureThreshold": 5,
          "BreakDuration": "00:01:00",
          "MinimumThroughput": 10
        },
        "Timeout": {
          "DefaultTimeout": "00:00:30"
        }
      }
    },
    "Observability": {
      "Performance": {
        "Enabled": true,
        "TrackMemoryUsage": true,
        "SampleSize": 1000
      },
      "HealthChecks": {
        "Enabled": true,
        "CheckInterval": "00:01:00",
        "Checks": {
          "Memory": { "Enabled": true, "ThresholdMB": 1024 },
          "GarbageCollector": { "Enabled": true },
          "ThreadPool": { "Enabled": true }
        }
      },
      "OpenTelemetry": {
        "Enabled": false,
        "ServiceName": "WorkflowForge",
        "ServiceVersion": "1.0.0",
        "Exporters": {
          "Console": { "Enabled": true },
          "Jaeger": { 
            "Enabled": false,
            "Endpoint": "http://localhost:14268"
          }
        }
      }
    }
  }
}
```

## Environment-Specific Configurations

### Development Configuration

**appsettings.Development.json:**
```json
{
  "WorkflowForge": {
    "Logging": {
      "MinimumLevel": "Debug",
      "Configuration": {
        "WriteTo": [
          { 
            "Name": "Console",
            "Args": {
              "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
            }
          }
        ]
      }
    },
    "Resilience": {
      "Polly": {
        "Retry": {
          "MaxAttempts": 1
        },
        "CircuitBreaker": {
          "Enabled": false
        }
      }
    },
    "Observability": {
      "Performance": {
        "Enabled": true,
        "TrackMemoryUsage": true,
        "DetailedMetrics": true
      },
      "HealthChecks": {
        "CheckInterval": "00:00:30"
      }
    }
  }
}
```

### Production Configuration

**appsettings.Production.json:**
```json
{
  "WorkflowForge": {
    "Core": {
      "MaxConcurrentWorkflows": 50
    },
    "Logging": {
      "MinimumLevel": "Information",
      "Configuration": {
        "WriteTo": [
          { 
            "Name": "File", 
            "Args": { 
              "path": "/var/log/workflow/workflow-.txt",
              "rollingInterval": "Hour",
              "retainedFileCountLimit": 168
            } 
          },
          {
            "Name": "Seq",
            "Args": {
              "serverUrl": "https://seq.company.com"
            }
          }
        ]
      }
    },
    "Resilience": {
      "Polly": {
        "Retry": {
          "MaxAttempts": 5,
          "BaseDelay": "00:00:02"
        },
        "CircuitBreaker": {
          "FailureThreshold": 10,
          "BreakDuration": "00:02:00"
        },
        "RateLimit": {
          "PermitLimit": 100,
          "Window": "00:00:01"
        }
      }
    },
    "Observability": {
      "Performance": {
        "Enabled": false
      },
      "HealthChecks": {
        "CheckInterval": "00:02:00"
      },
      "OpenTelemetry": {
        "Enabled": true,
        "Exporters": {
          "Console": { "Enabled": false },
          "Jaeger": { 
            "Enabled": true,
            "Endpoint": "https://jaeger.company.com"
          }
        }
      }
    }
  }
}
```

### Enterprise Configuration

**appsettings.Enterprise.json:**
```json
{
  "WorkflowForge": {
    "Core": {
      "MaxConcurrentWorkflows": 100,
      "EnableAuditLogging": true,
      "CorrelationIdHeader": "X-Correlation-ID"
    },
    "Logging": {
      "Configuration": {
        "WriteTo": [
          {
            "Name": "Splunk",
            "Args": {
              "host": "splunk.company.com",
              "port": 8088,
              "token": "{SPLUNK_TOKEN}"
            }
          }
        ],
        "Enrich": [
          "FromLogContext",
          "WithMachineName",
          "WithEnvironmentUserName",
          "WithCorrelationId"
        ]
      }
    },
    "Security": {
      "EnableEncryption": true,
      "EncryptionKey": "{ENCRYPTION_KEY}",
      "RequireAuthentication": true,
      "AuditAllOperations": true
    },
    "Observability": {
      "Performance": {
        "Enabled": true,
        "ExportToMetrics": true,
        "MetricsEndpoint": "https://metrics.company.com"
      },
      "HealthChecks": {
        "CheckInterval": "00:01:00",
        "Checks": {
          "Database": { 
            "Enabled": true,
            "ConnectionString": "{DATABASE_CONNECTION}",
            "Timeout": "00:00:10"
          },
          "ExternalServices": { "Enabled": true },
          "DiskSpace": { 
            "Enabled": true,
            "ThresholdPercent": 85 
          }
        }
      },
      "OpenTelemetry": {
        "Enabled": true,
        "Sampling": {
          "Type": "TraceIdRatioBased",
          "Ratio": 0.1
        },
        "Exporters": {
          "OTLP": {
            "Enabled": true,
            "Endpoint": "https://otel-collector.company.com"
          }
        }
      }
    }
  }
}
```

## Programmatic Configuration

### Fluent Configuration API

```csharp
using WorkflowForge;
using WorkflowForge.Extensions.Logging.Serilog;
using WorkflowForge.Extensions.Resilience.Polly;
using WorkflowForge.Extensions.Observability.Performance;

public static class FoundryConfigurations
{
    public static IWorkflowFoundry CreateDevelopmentFoundry(string name)
    {
        var config = FoundryConfiguration.ForDevelopment().UseSerilog(CreateDevelopmentLogger());
        var foundry = WorkflowForge.CreateFoundry(name, config)
            .UsePollyDevelopmentResilience();
        // Enable performance monitoring
        foundry.EnablePerformanceMonitoring();
        // Health checks service
        var healthService = foundry.CreateHealthCheckService(TimeSpan.FromSeconds(30));
        return foundry;
    }

    public static IWorkflowFoundry CreateOptimizedFoundry(string name)
    {
        var config = FoundryConfiguration.ForProduction().UseSerilog(CreateOptimizedLogger());
        var foundry = WorkflowForge.CreateFoundry(name, config)
            .UsePollyProductionResilience();
        // Enable OpenTelemetry with available options type
        foundry.EnableOpenTelemetry(new WorkflowForge.Extensions.Observability.OpenTelemetry.WorkflowForgeOpenTelemetryOptions
        {
            ServiceName = "OptimizedService",
            ServiceVersion = "1.0.0"
        });
        return foundry;
    }

    public static IWorkflowFoundry CreateAdvancedFoundry(string name, IServiceProvider serviceProvider)
    {
        var config = FoundryConfiguration.ForProduction().UseSerilog(CreateAdvancedLogger());
        var foundry = WorkflowForge.CreateFoundry(name, config)
            .UsePollyEnterpriseResilience();
        foundry.EnablePerformanceMonitoring();
        var healthService = foundry.CreateHealthCheckService(TimeSpan.FromMinutes(1));
        foundry.EnableOpenTelemetry(new WorkflowForge.Extensions.Observability.OpenTelemetry.WorkflowForgeOpenTelemetryOptions
        {
            ServiceName = "AdvancedService",
            ServiceVersion = "1.0.0"
        });
        return foundry;
    }

    private static ILogger CreateDevelopmentLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();
    }

    private static ILogger CreateOptimizedLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("/var/log/workflow/workflow-.txt", 
                rollingInterval: RollingInterval.Hour,
                retainedFileCountLimit: 168)
            .WriteTo.Seq("https://seq.company.com")
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .CreateLogger();
    }

    private static ILogger CreateAdvancedLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Splunk("splunk.company.com", 8088, Environment.GetEnvironmentVariable("SPLUNK_TOKEN"))
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentUserName()
            .Enrich.WithCorrelationId()
            .CreateLogger();
    }
}
```

### Configuration Factory Pattern

```csharp
public interface IFoundryConfigurationFactory
{
    IWorkflowFoundry CreateFoundry(string name, string environment);
}

public class FoundryConfigurationFactory : IFoundryConfigurationFactory
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public FoundryConfigurationFactory(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public IWorkflowFoundry CreateFoundry(string name, string environment)
    {
        var foundry = WorkflowForge.CreateFoundry(name, _serviceProvider);

        // Apply environment-specific configuration
        return environment.ToLowerInvariant() switch
        {
            "development" => ConfigureForDevelopment(foundry),
            "staging" => ConfigureForStaging(foundry),
            "optimized" => ConfigureForOptimized(foundry),
            "advanced" => ConfigureForAdvanced(foundry),
            _ => throw new ArgumentException($"Unknown environment: {environment}")
        };
    }

    private IWorkflowFoundry ConfigureForDevelopment(IWorkflowFoundry foundry)
    {
        var config = _configuration.GetSection("WorkflowForge:Development");
        
        return foundry
            .ConfigureFromSection(config)
            .UseSerilog()
            .UsePollyDevelopmentResilience()
            .EnablePerformanceMonitoring()
            .EnableHealthChecks();
    }

    // Other environment configurations...
}
```

## Configuration Validation

### Strongly-Typed Configuration

```csharp
public class WorkflowForgeSettings
{
    public const string SectionName = "WorkflowForge";

    [Required]
    public CoreSettings Core { get; set; } = new();
    
    public LoggingSettings Logging { get; set; } = new();
    
    public ResilienceSettings Resilience { get; set; } = new();
    
    public ObservabilitySettings Observability { get; set; } = new();
}

public class CoreSettings
{
    [Range(1, 1000)]
    public int MaxConcurrentWorkflows { get; set; } = 10;
    
    [Required]
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);
    
    public bool EnableCompensation { get; set; } = true;
}

public class LoggingSettings
{
    [Required]
    public string Provider { get; set; } = "Serilog";
    
    public string MinimumLevel { get; set; } = "Information";
    
    public Dictionary<string, object> Configuration { get; set; } = new();
}
```

### Configuration Validation

```csharp
public static class ConfigurationExtensions
{
    public static IServiceCollection AddWorkflowForgeConfiguration(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Configure with validation
        services.Configure<WorkflowForgeSettings>(configuration.GetSection(WorkflowForgeSettings.SectionName));
        
        // Add validation
        services.AddSingleton<IValidateOptions<WorkflowForgeSettings>, WorkflowForgeSettingsValidator>();
        
        return services;
    }
}

public class WorkflowForgeSettingsValidator : IValidateOptions<WorkflowForgeSettings>
{
    public ValidateOptionsResult Validate(string name, WorkflowForgeSettings options)
    {
        var errors = new List<string>();

        if (options.Core.MaxConcurrentWorkflows <= 0)
            errors.Add("MaxConcurrentWorkflows must be greater than 0");

        if (options.Core.DefaultTimeout <= TimeSpan.Zero)
            errors.Add("DefaultTimeout must be greater than zero");

        if (string.IsNullOrEmpty(options.Logging.Provider))
            errors.Add("Logging provider must be specified");

        return errors.Any() 
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
```

## Configuration Best Practices

### 1. Use Configuration Profiles

```csharp
public static class ConfigurationProfiles
{
    public static readonly FoundryProfile Development = new()
    {
        LogLevel = LogLevel.Debug,
        EnablePerformanceMonitoring = true,
        EnableDetailedLogging = true,
        MaxRetryAttempts = 1,
        EnableCircuitBreaker = false
    };

    public static readonly FoundryProfile Production = new()
    {
        LogLevel = LogLevel.Information,
        EnablePerformanceMonitoring = false,
        EnableDetailedLogging = false,
        MaxRetryAttempts = 5,
        EnableCircuitBreaker = true
    };
}
```

### 2. Environment Variable Overrides

```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables("WORKFLOWFORGE_")
    .Build();
```

**Environment Variables:**
```bash
WORKFLOWFORGE_Core__MaxConcurrentWorkflows=50
WORKFLOWFORGE_Logging__MinimumLevel=Warning
WORKFLOWFORGE_Resilience__Polly__Retry__MaxAttempts=3
```

### 3. Secrets Management

```csharp
// Azure Key Vault
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddAzureKeyVault(keyVaultEndpoint, credential)
    .Build();

// AWS Systems Manager
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddSystemsManager(configureSource => 
    {
        configureSource.Path = "/workflowforge/";
        configureSource.Optional = true;
    })
    .Build();
```

### 4. Configuration Hot Reload

```csharp
public class ConfigurableFoundryService
{
    private readonly IOptionsMonitor<WorkflowForgeSettings> _optionsMonitor;
    private IDisposable? _optionsChangeListener;

    public ConfigurableFoundryService(IOptionsMonitor<WorkflowForgeSettings> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
        _optionsChangeListener = _optionsMonitor.OnChange(OnConfigurationChanged);
    }

    private void OnConfigurationChanged(WorkflowForgeSettings newSettings)
    {
        // Reconfigure foundries with new settings
        ReconfigureFoundries(newSettings);
    }

    public void Dispose()
    {
        _optionsChangeListener?.Dispose();
    }
}
```

## Configuration Examples

### Microservices Configuration

```json
{
  "WorkflowForge": {
    "Core": {
      "ServiceName": "OrderProcessingService",
      "MaxConcurrentWorkflows": 20
    },
    "Observability": {
      "OpenTelemetry": {
        "Enabled": true,
        "ServiceName": "order-processing",
        "ServiceVersion": "2.1.0",
        "Exporters": {
          "OTLP": {
            "Enabled": true,
            "Endpoint": "http://jaeger-collector:14268"
          }
        }
      }
    },
    "Resilience": {
      "Polly": {
        "CircuitBreaker": {
          "FailureThreshold": 5,
          "BreakDuration": "00:01:00"
        }
      }
    }
  }
}
```

### High-Throughput Configuration

```json
{
  "WorkflowForge": {
    "Core": {
      "MaxConcurrentWorkflows": 200,
      "DefaultTimeout": "00:00:30"
    },
    "Logging": {
      "MinimumLevel": "Warning"
    },
    "Observability": {
      "Performance": {
        "Enabled": true,
        "SampleSize": 10000,
        "TrackMemoryUsage": false
      }
    }
  }
}
```

### Compliance Configuration

```json
{
  "WorkflowForge": {
    "Security": {
      "EnableEncryption": true,
      "RequireAuthentication": true,
      "AuditAllOperations": true,
      "DataRetentionDays": 2555
    },
    "Logging": {
      "Configuration": {
        "WriteTo": [
          {
            "Name": "AuditLog",
            "Args": {
              "path": "/audit/workflow-audit-.txt",
              "rollingInterval": "Day",
              "retainedFileCountLimit": 2555
            }
          }
        ]
      }
    }
  }
}
```

## Troubleshooting Configuration

### Common Configuration Issues

1. **Invalid JSON syntax**
   ```bash
   # Validate JSON syntax
   cat appsettings.json | jq .
   ```

2. **Environment variable not loading**
   ```csharp
   // Debug configuration sources
   foreach (var source in configuration.AsEnumerable())
   {
       Console.WriteLine($"{source.Key} = {source.Value}");
   }
   ```

3. **Missing required configuration**
   ```csharp
   // Use IValidateOptions<T> for validation
   services.AddSingleton<IValidateOptions<WorkflowForgeSettings>, WorkflowForgeSettingsValidator>();
   ```

## Related Documentation

- **[Getting Started](getting-started.md)** - Basic configuration setup
- **[Architecture](architecture.md)** - Configuration architecture
- **[Extensions](extensions.md)** - Extension configuration
- **[Troubleshooting](troubleshooting.md)** - Configuration issues

---

**WorkflowForge Configuration** - *Configure workflows for every environment* 