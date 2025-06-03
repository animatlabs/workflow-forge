// This file serves as documentation for the common operations available in WorkflowForge.
// The operations have been split into separate files for better organization:
//
// - LogLevel.cs: Defines log levels for workflow operations
// - LoggingOperation.cs: Simple logging operation for debugging and workflow tracking
// - DelayOperation.cs: Simple delay operation for workflow pacing and testing
//
// Usage Examples:
//
// Logging Operations:
// var debugLog = LoggingOperation.Debug("Processing started");
// var infoLog = LoggingOperation.Info("User authenticated");
// var errorLog = LoggingOperation.Error("Validation failed");
//
// Delay Operations:
// var shortDelay = DelayOperation.FromMilliseconds(500);
// var mediumDelay = DelayOperation.FromSeconds(5);
// var longDelay = DelayOperation.FromMinutes(1);

namespace WorkflowForge.Operations
{
    // This namespace contains common workflow operations that can be used
    // in workflow definitions. Each operation is in its own file for clarity.
} 
