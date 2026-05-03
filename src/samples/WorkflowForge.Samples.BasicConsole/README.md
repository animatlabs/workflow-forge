# WorkflowForge Basic Console samples

Console menu for **WorkflowForge 2.1.1**. Run a single sample or queue them all.

## Run

```bash
cd src/samples/WorkflowForge.Samples.BasicConsole
dotnet run
```

## Samples (33)

### Basic (1–4)

- **1. Hello world:** smallest workflow  
- **2. Data passing:** `foundry.Properties` between steps  
- **3. Multiple outcomes:** branching results  
- **4. Class-based operations:** preferred default for real code  

### Control flow (5–8)

- **5. Conditional workflows:** `ConditionalWorkflowOperation`  
- **6. ForEach loops:** parallel or sequential over collections  
- **7. Error handling:** exceptions and compensation  
- **8. Built-in operations:** logging, delays, helpers  

### Configuration and middleware (9–12)

- **9. Options pattern:** `IOptions<T>` wiring  
- **10. Configuration profiles:** dev vs prod style toggles  
- **11. Workflow events:** lifecycle, operations, compensation  
- **12. Middleware:** operation-level cross-cutting  

### Extensions (13–18, 21–25)

- **13. Serilog logging:** structured logs (ILRepack-isolated Serilog)  
- **14. Polly resilience:** retry, breaker, timeout (ILRepack-isolated Polly)  
- **15. OpenTelemetry:** traces toward Jaeger and friends  
- **16. Health checks:** host health integration  
- **17. Performance monitoring:** per-operation timing  
- **18. Persistence:** checkpoints with your storage provider  
- **21. Recovery only:** resume without standing up the full persistence stack  
- **22. Resilience + recovery:** Polly combined with recovery paths  
- **23. Validation:** DataAnnotations against foundry state  
- **24. Audit:** pluggable audit sinks  
- **25. Configuration-driven:** turn extensions on or off from `appsettings.json`  

### Onboarding (26–33)

- **26. Dependency injection:** register and resolve `IWorkflowSmith`  
- **27. Workflow middleware:** scope middleware to the whole workflow  
- **28. Cancellation + timeout:** tokens and timeout middleware  
- **29. Continue on error:** collect failures, keep moving  
- **30. Compensation behaviors:** success vs failure rollback  
- **31. Foundry reuse:** one foundry, many workflows  
- **32. Output chaining:** feed one operation’s output into the next  
- **33. Service provider access:** resolve dependencies inside operations  

### Advanced (19–20)

- **19. Full integration run:** every extension exercised together  
- **20. Operation creation patterns:** each supported way to build operations  

## Menu

- **1–33:** single sample  
- **A:** run all samples in order  
- **B:** basics only (1–4)  
- **Q:** quit  

Most samples narrate on the console, print timings where it matters, and failure-oriented demos actually fail on purpose.

## Configuration

`appsettings.json` holds environment-specific knobs: logging levels, resilience policies, extension switches.

## Path through the menu

Start with **1–4**, add **5–12** for control flow and wiring, then **13–33** for extensions and hosting notes. **19–20** are the widest end-to-end passes.

## What is covered

- **Core:** workflows, operations, `foundry.Properties`  
- **Control flow:** conditionals, `ForEach`, errors, compensation  
- **Configuration:** `IOptions<T>`, profiles, middleware  
- **Events:** workflow, operation, and compensation hooks  
- **Extensions:** eleven extension packages plus Testing, each with a clear dependency boundary  
- **Resilience:** Polly retry, breaker, timeout samples  
- **Observability:** logging, tracing, health, timing  
- **Persistence:** checkpoint and resume  
- **Validation:** annotations against foundry state  
- **Audit:** swappable audit sinks  
- **Guidance:** DI, reuse, chaining, service resolution  

## Docs

[Samples guide](../../../docs/getting-started/samples-guide.md) for a full walkthrough of every number in the menu.
