# WorkflowForge samples

Runnable console projects you can step through locally.

## `WorkflowForge.Samples.BasicConsole`

**33** menu-driven examples:

- **Basic (1–4):** hello world, data through `foundry.Properties`, branching, class-based operations  
- **Control flow (5–8):** conditionals, `ForEach`, errors, built-in ops  
- **Config + middleware (9–12):** options pattern, configuration profiles, events, middleware  
- **Extensions (13–18, 21–25):** Serilog, Polly, OpenTelemetry, health checks, performance hooks, persistence, recovery, validation, audit, configuration-driven wiring  
- **Advanced (19–20):** all extensions in one pass; ways to construct operations  
- **Onboarding (26–33):** DI, workflow middleware, cancellation/timeouts, continue-on-error, compensation, foundry reuse, output chaining, service resolution  

### Run it

```bash
cd WorkflowForge.Samples.BasicConsole
dotnet run
```

Use the menu for a single sample or walk the list in order.

### What you will see

- Core engine: `foundry.Properties`, compensation, middleware, lifecycle events  
- All official extension packages (plus Testing) without version clashes  
- `appsettings.json` wiring you can copy into real services  

## Suggested order

1. New to WorkflowForge: samples **1–4** ([BasicConsole README](WorkflowForge.Samples.BasicConsole/README.md))  
2. Flow + configuration: **5–12**  
3. Extensions + hosting patterns: **13–33**  

## Docs

- [Samples guide](../../docs/getting-started/samples-guide.md)  
- [Getting started](../../docs/getting-started/getting-started.md)  
- [Operations](../../docs/core/operations.md)  
- [Configuration](../../docs/core/configuration.md)  
- [Extensions](../../docs/extensions/index.md)  

## Prerequisites

- .NET 8.0 SDK or later  
- Windows, Linux, or macOS  

## Run everything

```bash
cd WorkflowForge.Samples.BasicConsole
dotnet run
# Choose 'A' in the menu for the full set
```

Pick **1–33** for one sample.

## Contributing samples

See [CONTRIBUTING.md](../../CONTRIBUTING.md). Each sample should show one idea clearly, print useful output, stay self-contained, and follow the existing layout.
