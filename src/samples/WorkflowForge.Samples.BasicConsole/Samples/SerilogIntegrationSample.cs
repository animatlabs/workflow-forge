using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Extensions.Logging.Serilog;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates Serilog integration with WorkflowForge for enterprise-grade structured logging.
/// Shows rich context, custom properties, and log enrichment patterns.
/// </summary>
public class SerilogIntegrationSample : ISample
{
    public string Name => "Serilog Integration";
    public string Description => "Enterprise structured logging with Serilog extension";

    public async Task RunAsync()
    {
        Console.WriteLine("Setting up Serilog for structured logging...");

        var logger = SerilogLoggerFactory.CreateLogger(new SerilogLoggerOptions
        {
            MinimumLevel = "Debug",
            ConsoleOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        });

        Console.WriteLine("Running workflow with structured logging...");

        using var foundry = WorkflowForge.CreateFoundry("SerilogIntegration", logger);

        // Add workflow data for context
        foundry.Properties["user_id"] = "usr_12345";
        foundry.Properties["session_id"] = Guid.NewGuid().ToString("N")[..8];
        foundry.Properties["correlation_id"] = Guid.NewGuid().ToString();

        foundry
            .WithOperation(new UserRegistrationOperation())
            .WithOperation(new EmailNotificationOperation())
            .WithOperation(new AuditLoggingOperation());

        await foundry.ForgeAsync();
    }
}

/// <summary>
/// User registration operation with rich structured logging
/// </summary>
public class UserRegistrationOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "UserRegistration";
    public bool SupportsRestore => true;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var userId = foundry.Properties["user_id"] as string ?? "unknown";
        var sessionId = foundry.Properties["session_id"] as string ?? "unknown";

        // Create a logging scope with operation context
        using var scope = foundry.Logger.BeginScope(Name, new Dictionary<string, string>
        {
            ["UserId"] = userId,
            ["SessionId"] = sessionId,
            ["Operation"] = "ProcessRegistration",
            ["Step"] = "Validation"
        });

        foundry.Logger.LogInformation("Starting user registration process for user {UserId}", userId);

        // Simulate user data
        var userData = new
        {
            UserId = userId,
            Email = "john.doe@example.com",
            UserType = "Standard",
            RegistrationSource = "WebPortal",
            Timestamp = DateTime.UtcNow
        };

        // Log with custom properties
        var properties = new Dictionary<string, string>
        {
            ["Email"] = userData.Email,
            ["UserType"] = userData.UserType,
            ["RegistrationSource"] = userData.RegistrationSource
        };

        foundry.Logger.LogInformation(properties,
            "Processing user registration for {Email}", userData.Email);

        // Simulate validation steps
        await Task.Delay(100, cancellationToken);

        foundry.Logger.LogDebug("User validation completed successfully");

        // Simulate some processing
        await Task.Delay(150, cancellationToken);

        // Store result for next operation
        foundry.Properties["registration_result"] = userData;
        foundry.Properties["registration_completed"] = DateTime.UtcNow;

        foundry.Logger.LogInformation(properties,
            "User registration completed successfully for {Email} in {Duration}ms",
            userData.Email, 250);

        return userData;
    }

    public async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var userId = foundry.Properties["user_id"] as string ?? "unknown";

        foundry.Logger.LogWarning("Restoring user registration for user {UserId} due to workflow failure", userId);

        // Simulate cleanup
        await Task.Delay(50, cancellationToken);

        foundry.Properties.TryRemove("registration_result", out _);
        foundry.Properties.TryRemove("registration_completed", out _);

        foundry.Logger.LogInformation("User registration restoration completed for user {UserId}", userId);
    }

    public void Dispose()
    { }
}

/// <summary>
/// Email notification operation with error simulation and logging
/// </summary>
public class EmailNotificationOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "EmailNotification";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var userData = inputData as dynamic;
        var correlationId = foundry.Properties["correlation_id"] as string ?? "unknown";

        using var scope = foundry.Logger.BeginScope(Name, new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId,
            ["Operation"] = "SendWelcomeEmail",
            ["Template"] = "WelcomeEmail"
        });

        foundry.Logger.LogInformation("Sending welcome email to {Email}", userData?.Email ?? "unknown");

        try
        {
            // Simulate email sending
            await Task.Delay(200, cancellationToken);

            var messageId = $"msg_{Guid.NewGuid().ToString("N")[..8]}";
            var deliveryTime = 200;

            var emailProperties = new Dictionary<string, string>
            {
                ["MessageId"] = messageId,
                ["Template"] = "WelcomeEmail",
                ["Priority"] = "Normal",
                ["DeliveryTime"] = $"{deliveryTime}ms"
            };

            foundry.Logger.LogInformation(emailProperties,
                "Email sent successfully to {Email} with message ID {MessageId}",
                userData?.Email ?? "unknown", messageId);

            foundry.Properties["email_message_id"] = messageId;
            foundry.Properties["email_sent_at"] = DateTime.UtcNow;

            return new { MessageId = messageId, SentAt = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            var errorProperties = new Dictionary<string, string>
            {
                ["ErrorCode"] = "SMTP_TIMEOUT",
                ["SmtpServer"] = "smtp.example.com",
                ["AttemptNumber"] = "1"
            };

            foundry.Logger.LogError(errorProperties, ex,
                "Email delivery failed for user {Email}", userData?.Email ?? "unknown");

            throw;
        }
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Email notification does not support restoration");
    }

    public void Dispose()
    { }
}

/// <summary>
/// Audit logging operation demonstrating comprehensive audit trails
/// </summary>
public class AuditLoggingOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "AuditLogging";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        var userId = foundry.Properties["user_id"] as string ?? "unknown";
        var correlationId = foundry.Properties["correlation_id"] as string ?? "unknown";
        var registrationCompleted = foundry.Properties["registration_completed"] as DateTime?;
        var emailMessageId = foundry.Properties["email_message_id"] as string;

        using var scope = foundry.Logger.BeginScope(Name, new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId,
            ["AuditType"] = "UserRegistration",
            ["ComplianceLevel"] = "Standard"
        });

        foundry.Logger.LogInformation("Recording audit trail for user registration {UserId}", userId);

        // Create comprehensive audit entry
        var auditEntry = new
        {
            UserId = userId,
            Action = "UserRegistered",
            Timestamp = DateTime.UtcNow,
            CorrelationId = correlationId,
            Details = new
            {
                RegistrationCompleted = registrationCompleted,
                EmailSent = emailMessageId != null,
                EmailMessageId = emailMessageId,
                WorkflowExecutionId = foundry.ExecutionId,
                ProcessingDuration = registrationCompleted.HasValue
                    ? (DateTime.UtcNow - registrationCompleted.Value).TotalMilliseconds
                    : 0
            }
        };

        // Log audit entry with rich properties
        var auditProperties = new Dictionary<string, string>
        {
            ["Action"] = auditEntry.Action,
            ["AuditTimestamp"] = auditEntry.Timestamp.ToString("O"),
            ["WorkflowExecutionId"] = foundry.ExecutionId.ToString(),
            ["EmailSent"] = auditEntry.Details.EmailSent.ToString(),
            ["ProcessingDuration"] = $"{auditEntry.Details.ProcessingDuration}ms"
        };

        foundry.Logger.LogInformation(auditProperties,
            "Audit trail recorded for user {UserId} - Action: {Action}, Duration: {ProcessingDuration}ms",
            userId, auditEntry.Action, auditEntry.Details.ProcessingDuration);

        // Simulate audit storage
        await Task.Delay(75, cancellationToken);

        foundry.Properties["audit_entry"] = auditEntry;

        foundry.Logger.LogDebug("Audit entry stored successfully for correlation ID {CorrelationId}", correlationId);

        return auditEntry;
    }

    public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Audit logging does not support restoration");
    }

    public void Dispose()
    { }
}