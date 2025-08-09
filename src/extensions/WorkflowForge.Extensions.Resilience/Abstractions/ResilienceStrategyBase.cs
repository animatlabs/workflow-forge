using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Extensions.Resilience.Abstractions
{
    /// <summary>
    /// Base class for resilience strategies that provides common functionality.
    /// </summary>
    public abstract class ResilienceStrategyBase : IWorkflowResilienceStrategy
    {
        private readonly string _name;
        private readonly IWorkflowForgeLogger? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResilienceStrategyBase"/> class.
        /// </summary>
        /// <param name="name">The name of the strategy.</param>
        /// <param name="logger">The optional logger.</param>
        protected ResilienceStrategyBase(string name, IWorkflowForgeLogger? logger = null)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => _name;

        /// <summary>
        /// Gets the logger for this strategy.
        /// </summary>
        protected IWorkflowForgeLogger? Logger => _logger;

        /// <inheritdoc />
        public abstract Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);

        /// <inheritdoc />
        public abstract Task<bool> ShouldRetryAsync(int attemptNumber, Exception? exception, CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract TimeSpan GetRetryDelay(int attemptNumber, Exception? exception);

        /// <inheritdoc />
        public virtual async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            T result = default!;
            await ExecuteAsync(async () => { result = await operation().ConfigureAwait(false); }, cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}