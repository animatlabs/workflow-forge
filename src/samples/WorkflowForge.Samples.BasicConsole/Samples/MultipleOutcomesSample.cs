using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions;
using WorkflowForge.Operations;

namespace WorkflowForge.Samples.BasicConsole.Samples;

/// <summary>
/// Demonstrates operations with multiple outcomes and subsequent conditional processing.
/// Shows how to handle different operation results and branch accordingly.
/// </summary>
public class MultipleOutcomesSample : ISample
{
    public string Name => "Multiple Outcomes Workflow";
    public string Description => "Demonstrates operations with multiple outcomes and branching";

    public async Task RunAsync()
    {
        Console.WriteLine("Creating a workflow that demonstrates multiple outcomes...");
        
        using var foundry = WorkflowForge.CreateFoundry("MultipleOutcomesWorkflow", FoundryConfiguration.Development());
        
        // Set test data for different scenarios
        foundry.Properties["credit_score"] = 720;
        foundry.Properties["income"] = 85000m;
        foundry.Properties["debt_ratio"] = 0.25m;
        
        Console.WriteLine($"Applicant data - Credit Score: {foundry.Properties["credit_score"]}, Income: ${foundry.Properties["income"]:N0}, Debt Ratio: {foundry.Properties["debt_ratio"]:P0}");
        
        foundry
            .WithOperation(new CreditCheckOperation())
            .WithOperation(new ConditionalWorkflowOperation(
                // Check the outcome from credit check
                (inputData, foundry, cancellationToken) => Task.FromResult(((string)foundry.Properties["credit_decision"]!).Equals("APPROVED", StringComparison.OrdinalIgnoreCase)),
                // If approved path (composite operation)
                ForEachWorkflowOperation.CreateSharedInput(new IWorkflowOperation[]
                {
                    new LoggingOperation("[SUCCESS] Credit check passed - proceeding with loan processing"),
                    new LoanProcessingOperation()
                }, name: "ApprovedPath"),
                // If rejected path (composite operation)
                ForEachWorkflowOperation.CreateSharedInput(new IWorkflowOperation[]
                {
                    new LoggingOperation("[ERROR] Credit check failed"),
                    new ConditionalWorkflowOperation(
                        // Check if it's a soft decline (can be reconsidered)
                        (inputData, foundry, cancellationToken) => Task.FromResult(((string)foundry.Properties["decline_reason"]!).Contains("Income")),
                        // Soft decline - offer alternatives
                        new OfferAlternativesOperation(),
                        // Hard decline - final rejection
                        new LoggingOperation("🚫 Hard decline - no alternatives available")
                    )
                }, name: "RejectedPath")))
            .WithOperation(new ConditionalWorkflowOperation(
                // Check loan processing outcome (only if credit was approved)
                (inputData, foundry, cancellationToken) => Task.FromResult(
                    foundry.Properties.ContainsKey("loan_decision") && 
                    ((string)foundry.Properties["loan_decision"]!).Equals("APPROVED", StringComparison.OrdinalIgnoreCase)),
                // If loan approved
                new GenerateLoanDocumentsOperation(),
                // If loan rejected or not processed
                new LoggingOperation("[ERROR] Loan processing failed or not applicable - sending rejection notice")))
            .WithOperation(new NotifyApplicantOperation());
        
        Console.WriteLine("\nExecuting workflow with multiple outcome paths...");
        await foundry.ForgeAsync();
    }
}

public class CreditCheckOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "CreditCheck";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [INFO] Performing credit check...");
        
        // Simulate credit check processing
        await Task.Delay(150, cancellationToken);
        
        var creditScore = (int)foundry.Properties["credit_score"]!;
        var income = (decimal)foundry.Properties["income"]!;
        var debtRatio = (decimal)foundry.Properties["debt_ratio"]!;
        
        // Multiple outcome logic
        string decision;
        string reason;
        
        if (creditScore >= 750 && income >= 50000m && debtRatio <= 0.3m)
        {
            decision = "APPROVED";
            reason = "Excellent credit profile";
        }
        else if (creditScore >= 650 && income >= 30000m && debtRatio <= 0.4m)
        {
            decision = "APPROVED";
            reason = "Good credit profile with conditions";
        }
        else if (creditScore >= 600 && income >= 40000m)
        {
            decision = "DECLINED";
            reason = "Income insufficient for debt ratio";
        }
        else if (creditScore < 600)
        {
            decision = "DECLINED";
            reason = "Credit score below minimum threshold";
        }
        else
        {
            decision = "DECLINED";
            reason = "Multiple risk factors identified";
        }
        
        foundry.Properties["credit_decision"] = decision;
        foundry.Properties["decline_reason"] = reason;
        foundry.Properties["credit_check_date"] = DateTime.UtcNow;
        
        Console.WriteLine($"   [INFO] Credit decision: {decision} - {reason}");
        
        return decision;
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        foundry.Properties.TryRemove("credit_decision", out _);
        foundry.Properties.TryRemove("decline_reason", out _);
        foundry.Properties.TryRemove("credit_check_date", out _);
        return Task.CompletedTask;
    }

    public void Dispose() { }
}

public class LoanProcessingOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "LoanProcessing";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [INFO] Processing loan application...");
        
        await Task.Delay(200, cancellationToken);
        
        var income = (decimal)foundry.Properties["income"]!;
        var debtRatio = (decimal)foundry.Properties["debt_ratio"]!;
        
        // Loan-specific processing with multiple outcomes
        string loanDecision;
        decimal maxLoanAmount;
        decimal interestRate;
        
        if (income >= 100000m && debtRatio <= 0.2m)
        {
            loanDecision = "APPROVED";
            maxLoanAmount = income * 5m;
            interestRate = 3.5m;
        }
        else if (income >= 60000m && debtRatio <= 0.3m)
        {
            loanDecision = "APPROVED";
            maxLoanAmount = income * 3m;
            interestRate = 4.2m;
        }
        else if (income >= 40000m && debtRatio <= 0.35m)
        {
            loanDecision = "APPROVED";
            maxLoanAmount = income * 2m;
            interestRate = 5.1m;
        }
        else
        {
            loanDecision = "DECLINED";
            maxLoanAmount = 0m;
            interestRate = 0m;
        }
        
        foundry.Properties["loan_decision"] = loanDecision;
        foundry.Properties["max_loan_amount"] = maxLoanAmount;
        foundry.Properties["interest_rate"] = interestRate;
        foundry.Properties["loan_processing_date"] = DateTime.UtcNow;
        
        if (loanDecision == "APPROVED")
        {
            Console.WriteLine($"   [SUCCESS] Loan approved! Max amount: ${maxLoanAmount:N0} at {interestRate}% APR");
        }
        else
        {
            Console.WriteLine($"   [ERROR] Loan declined during processing");
        }
        
        return loanDecision;
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        foundry.Properties.TryRemove("loan_decision", out _);
        foundry.Properties.TryRemove("max_loan_amount", out _);
        foundry.Properties.TryRemove("interest_rate", out _);
        foundry.Properties.TryRemove("loan_processing_date", out _);
        return Task.CompletedTask;
    }

    public void Dispose() { }
}

public class GenerateLoanDocumentsOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "GenerateLoanDocuments";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [INFO] Generating loan documents...");
        
        await Task.Delay(100, cancellationToken);
        
        var loanAmount = (decimal)foundry.Properties["max_loan_amount"]!;
        var interestRate = (decimal)foundry.Properties["interest_rate"]!;
        
        var documentId = $"LOAN-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        foundry.Properties["loan_document_id"] = documentId;
        foundry.Properties["final_status"] = "APPROVED_WITH_DOCUMENTS";
        
        Console.WriteLine($"   [INFO] Loan documents generated: {documentId}");
        Console.WriteLine($"   [INFO] Final loan terms: ${loanAmount:N0} at {interestRate}% APR");
        
        return $"Documents generated: {documentId}";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        foundry.Properties.TryRemove("loan_document_id", out _);
        foundry.Properties.TryRemove("final_status", out _);
        return Task.CompletedTask;
    }

    public void Dispose() { }
}

public class OfferAlternativesOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "OfferAlternatives";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [INFO] Generating alternative offers...");
        
        await Task.Delay(120, cancellationToken);
        
        var income = (decimal)foundry.Properties["income"]!;
        
        // Offer alternative products
        var alternatives = new[]
        {
            $"Secured credit card with ${Math.Min(income * 0.1m, 2000m):N0} limit",
            "Financial counseling services",
            "Reapply in 6 months with improved credit"
        };
        
        foundry.Properties["alternatives_offered"] = alternatives;
        foundry.Properties["final_status"] = "DECLINED_WITH_ALTERNATIVES";
        
        Console.WriteLine("   [INFO] Alternative options offered:");
        foreach (var alternative in alternatives)
        {
            Console.WriteLine($"      • {alternative}");
        }
        
        return "Alternatives offered";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        foundry.Properties.TryRemove("alternatives_offered", out _);
        foundry.Properties.TryRemove("final_status", out _);
        return Task.CompletedTask;
    }

    public void Dispose() { }
}

public class NotifyApplicantOperation : IWorkflowOperation
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name => "NotifyApplicant";
    public bool SupportsRestore => false;

    public async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("   [INFO] Sending notification to applicant...");
        
        await Task.Delay(80, cancellationToken);
        
        var finalStatus = foundry.Properties.TryGetValue("final_status", out var status) ? (string)status! : "PROCESSED";
        
        foundry.Properties["notification_sent"] = true;
        foundry.Properties["notification_date"] = DateTime.UtcNow;
        
        var message = finalStatus switch
        {
            "APPROVED_WITH_DOCUMENTS" => "[SUCCESS] Congratulations! Your loan has been approved. Documents are being prepared.",
            "DECLINED_WITH_ALTERNATIVES" => "[INFO] While we cannot approve your loan at this time, we have alternative options for you.",
            _ => "[INFO] Your application has been processed. Please check your account for details."
        };
        
        Console.WriteLine($"   [INFO] {message}");
        
        return "Notification sent";
    }

    public Task RestoreAsync(object? context, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
    {
        foundry.Properties.TryRemove("notification_sent", out _);
        foundry.Properties.TryRemove("notification_date", out _);
        return Task.CompletedTask;
    }

    public void Dispose() { }
} 