using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Operations
{
    /// <summary>
    /// Abstract base class for workflow operations that provides default implementations
    /// and bridges between typed and untyped operation interfaces.
    /// 
    /// <para><strong>When to inherit from this class:</strong></para>
    /// <list type="bullet">
    /// <item>Creating custom operations with complex business logic</item>
    /// <item>Operations that need access to workflow context and foundry</item>
    /// <item>Operations that require custom initialization or state management</item>
    /// <item>Operations that need specific error handling or logging</item>
    /// </list>
    /// 
    /// <para><strong>For simple delegate-based operations, consider using:</strong></para>
    /// <list type="bullet">
    /// <item><see cref="DelegateWorkflowOperation"/> for untyped operations</item>
    /// <item><see cref="WorkflowOperations"/> factory class for convenience methods</item>
    /// </list>
    /// 
    /// <para><strong>Example custom operation:</strong></para>
    /// <code>
    /// public class FileProcessingOperation : WorkflowOperationBase
    /// {
    ///     public override string Name => "ProcessFile";
    ///     
    ///     public override async Task&lt;object?&gt; ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    ///     {
    ///         var filePath = (string)inputData;
    ///         // Complex file processing logic here
    ///         return processedData;
    ///     }
    /// }
    /// </code>
    /// </summary>
    public abstract class WorkflowOperationBase : IWorkflowOperation
    {
        /// <inheritdoc />
        public virtual Guid Id { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public virtual bool SupportsRestore => false;

        /// <inheritdoc />
        public abstract Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);

        /// <inheritdoc />
        public virtual Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (!SupportsRestore)
                throw new NotSupportedException($"Operation '{Name}' does not support restoration.");
            
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            // Override in derived classes if needed
        }
    }

    /// <summary>
    /// Abstract base class for strongly-typed workflow operations.
    /// 
    /// <para><strong>When to inherit from this class:</strong></para>
    /// <list type="bullet">
    /// <item>Creating typed operations with compile-time type safety</item>
    /// <item>Operations that work with specific input/output types</item>
    /// <item>Complex transformations that benefit from strong typing</item>
    /// <item>Operations that need to validate input/output types</item>
    /// </list>
    /// 
    /// <para><strong>For simple typed delegate-based operations, consider using:</strong></para>
    /// <list type="bullet">
    /// <item><see cref="DelegateWorkflowOperation{TInput, TOutput}"/> for typed delegate operations</item>
    /// <item><see cref="WorkflowOperations.Create{TInput, TOutput}(string, Func{TInput, TOutput})"/> factory method</item>
    /// </list>
    /// 
    /// <para><strong>Example custom typed operation:</strong></para>
    /// <code>
    /// public class DataTransformOperation : WorkflowOperationBase&lt;InputModel, OutputModel&gt;
    /// {
    ///     public override string Name => "TransformData";
    ///     
    ///     public override async Task&lt;OutputModel&gt; ForgeAsync(InputModel input, IWorkflowFoundry foundry, CancellationToken cancellationToken)
    ///     {
    ///         // Complex transformation logic here with full type safety
    ///         return new OutputModel { ProcessedData = input.RawData.ToUpper() };
    ///     }
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="TInput">The input data type.</typeparam>
    /// <typeparam name="TOutput">The output data type.</typeparam>
    public abstract class WorkflowOperationBase<TInput, TOutput> : WorkflowOperationBase, IWorkflowOperation<TInput, TOutput>
    {
        /// <summary>
        /// Strongly-typed implementation of the operation logic.
        /// This is the main method you should override in your custom operations.
        /// </summary>
        /// <param name="input">The typed input data.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The typed output data.</returns>
        public abstract Task<TOutput> ForgeAsync(TInput input, IWorkflowFoundry foundry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Strongly-typed restoration logic.
        /// Override this method if your operation supports restoration/rollback.
        /// </summary>
        /// <param name="output">The typed output data to restore.</param>
        /// <param name="foundry">The workflow foundry providing context and services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the restoration operation.</returns>
        public virtual Task RestoreAsync(TOutput output, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (!SupportsRestore)
                throw new NotSupportedException($"Operation '{Name}' does not support restoration.");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Implements the untyped interface by casting to/from the typed interface.
        /// DO NOT OVERRIDE THIS METHOD - it handles type conversion automatically.
        /// </summary>
        public sealed override async Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            // Handle type conversion and validation
            TInput typedInput;
            
            if (typeof(TInput) == typeof(object) || inputData is TInput directCast)
            {
                typedInput = (TInput)inputData!;
            }
            else if (inputData == null && !typeof(TInput).IsValueType)
            {
                typedInput = default(TInput)!;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Operation '{Name}' expects input of type {typeof(TInput).Name} but received {inputData?.GetType().Name ?? "null"}.");
            }

            var result = await ForgeAsync(typedInput, foundry, cancellationToken).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Implements the untyped restoration interface.
        /// DO NOT OVERRIDE THIS METHOD - it handles type conversion automatically.
        /// </summary>
        public sealed override async Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
        {
            if (!SupportsRestore)
                throw new NotSupportedException($"Operation '{Name}' does not support restoration.");

            // Handle type conversion for output data
            TOutput typedOutput;
            
            if (typeof(TOutput) == typeof(object) || outputData is TOutput directCast)
            {
                typedOutput = (TOutput)outputData!;
            }
            else if (outputData == null && !typeof(TOutput).IsValueType)
            {
                typedOutput = default(TOutput)!;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Operation '{Name}' expects output of type {typeof(TOutput).Name} for restoration but received {outputData?.GetType().Name ?? "null"}.");
            }

            await RestoreAsync(typedOutput, foundry, cancellationToken).ConfigureAwait(false);
        }
    }
} 
