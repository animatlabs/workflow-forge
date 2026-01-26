using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using WorkflowForge.Abstractions;
using WorkflowForge.Options;
using WorkflowForge.Options.Middleware;

namespace WorkflowForge.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring WorkflowForge services in dependency injection containers.
    /// Provides IOptions pattern integration for ASP.NET Core and other DI-based applications.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds WorkflowForge services to the service collection with configuration binding.
        /// Automatically binds core and middleware options from configuration sections and validates them.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration root.</param>
        /// <param name="coreSectionName">Optional custom section name for core options. Uses WorkflowForgeOptions.DefaultSectionName if null.</param>
        /// <param name="timingSectionName">Optional custom section name for timing middleware. Uses TimingMiddlewareOptions.DefaultSectionName if null.</param>
        /// <param name="loggingSectionName">Optional custom section name for logging middleware. Uses LoggingMiddlewareOptions.DefaultSectionName if null.</param>
        /// <param name="errorHandlingSectionName">Optional custom section name for error handling middleware. Uses ErrorHandlingMiddlewareOptions.DefaultSectionName if null.</param>
        /// <returns>The service collection for method chaining.</returns>
        /// <example>
        /// <code>
        /// // With default section names
        /// services.AddWorkflowForge(configuration);
        ///
        /// // With custom core section only
        /// services.AddWorkflowForge(configuration, coreSectionName: "MyApp:Workflows");
        ///
        /// // With all custom sections
        /// services.AddWorkflowForge(configuration,
        ///     coreSectionName: "MyApp:Workflows",
        ///     timingSectionName: "MyApp:Workflows:Timing",
        ///     loggingSectionName: "MyApp:Workflows:Logging");
        /// </code>
        /// </example>
        public static IServiceCollection AddWorkflowForge(
            this IServiceCollection services,
            IConfiguration configuration,
            string? coreSectionName = null,
            string? timingSectionName = null,
            string? loggingSectionName = null,
            string? errorHandlingSectionName = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            // Bind and validate core WorkflowForge options
            var coreSection = coreSectionName ?? WorkflowForgeOptions.DefaultSectionName;
            services.AddOptions<WorkflowForgeOptions>()
                .Bind(configuration.GetSection(coreSection))
                .Configure(opts =>
                {
                    // Store the actual section name used
                    // Note: Can't modify SectionName as it's read-only, handled via constructor
                })
                .Validate(opts =>
                {
                    var errors = opts.Validate();
                    return errors.Count == 0;
                },
                "WorkflowForge configuration validation failed");

            // Bind and validate middleware options with configurable section names
            var timingSection = timingSectionName ?? TimingMiddlewareOptions.DefaultSectionName;
            services.AddOptions<TimingMiddlewareOptions>()
                .Bind(configuration.GetSection(timingSection));

            var loggingSection = loggingSectionName ?? LoggingMiddlewareOptions.DefaultSectionName;
            services.AddOptions<LoggingMiddlewareOptions>()
                .Bind(configuration.GetSection(loggingSection))
                .Validate(opts =>
                {
                    var errors = opts.Validate();
                    return errors.Count == 0;
                },
                "Logging middleware configuration validation failed");

            var errorHandlingSection = errorHandlingSectionName ?? ErrorHandlingMiddlewareOptions.DefaultSectionName;
            services.AddOptions<ErrorHandlingMiddlewareOptions>()
                .Bind(configuration.GetSection(errorHandlingSection));

            // Register the options validator for startup-time validation
            services.AddSingleton<IValidateOptions<WorkflowForgeOptions>, WorkflowForgeOptionsValidator>();

            return services;
        }

        /// <summary>
        /// Adds WorkflowForge services with manual configuration.
        /// Use this when not using IConfiguration (e.g., in libraries or test scenarios).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureCore">Action to configure core WorkflowForge options.</param>
        /// <param name="configureTiming">Optional action to configure timing middleware options.</param>
        /// <param name="configureLogging">Optional action to configure logging middleware options.</param>
        /// <param name="configureErrorHandling">Optional action to configure error handling middleware options.</param>
        /// <returns>The service collection for method chaining.</returns>
        /// <example>
        /// <code>
        /// services.AddWorkflowForge(
        ///     core => {
        ///         core.MaxConcurrentOperations = 4;
        ///         core.MaxConcurrentWorkflows = 2;
        ///     },
        ///     timing => timing.Enabled = false,
        ///     logging => logging.MinimumLevel = "Warning"
        /// );
        /// </code>
        /// </example>
        public static IServiceCollection AddWorkflowForge(
            this IServiceCollection services,
            Action<WorkflowForgeOptions>? configureCore = null,
            Action<TimingMiddlewareOptions>? configureTiming = null,
            Action<LoggingMiddlewareOptions>? configureLogging = null,
            Action<ErrorHandlingMiddlewareOptions>? configureErrorHandling = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            // Configure and validate core options
            var coreBuilder = services.AddOptions<WorkflowForgeOptions>();
            if (configureCore != null)
            {
                coreBuilder.Configure(configureCore);
            }
            coreBuilder.Validate(opts =>
                {
                    var errors = opts.Validate();
                    return errors.Count == 0;
                },
                "WorkflowForge configuration validation failed");

            // Configure timing options
            if (configureTiming != null)
            {
                services.AddOptions<TimingMiddlewareOptions>().Configure(configureTiming);
            }
            else
            {
                services.AddOptions<TimingMiddlewareOptions>();
            }

            // Configure and validate logging options
            var loggingBuilder = services.AddOptions<LoggingMiddlewareOptions>();
            if (configureLogging != null)
            {
                loggingBuilder.Configure(configureLogging);
            }
            loggingBuilder.Validate(opts =>
                {
                    var errors = opts.Validate();
                    return errors.Count == 0;
                },
                "Logging middleware configuration validation failed");

            // Configure error handling options
            if (configureErrorHandling != null)
            {
                services.AddOptions<ErrorHandlingMiddlewareOptions>().Configure(configureErrorHandling);
            }
            else
            {
                services.AddOptions<ErrorHandlingMiddlewareOptions>();
            }

            // Register the options validator for startup-time validation
            services.AddSingleton<IValidateOptions<WorkflowForgeOptions>, WorkflowForgeOptionsValidator>();

            return services;
        }

        /// <summary>
        /// Adds WorkflowSmith to the service collection with automatic options resolution.
        /// Requires WorkflowForge options to be registered (via AddWorkflowForge) and IWorkflowForgeLogger to be registered.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for method chaining.</returns>
        /// <remarks>
        /// This registers WorkflowSmith as a singleton that reads from IOptions&lt;WorkflowForgeOptions&gt;.
        /// Ensure you have registered IWorkflowForgeLogger before calling this method.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Register logger first
        /// services.AddSingleton&lt;IWorkflowForgeLogger&gt;(sp =>
        ///     SerilogExtensions.CreateWorkflowForgeLogger());
        ///
        /// // Register WorkflowForge configuration
        /// services.AddWorkflowForge(configuration);
        ///
        /// // Register WorkflowSmith
        /// services.AddWorkflowSmith();
        /// </code>
        /// </example>
        public static IServiceCollection AddWorkflowSmith(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddSingleton<IWorkflowSmith>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<IWorkflowForgeLogger>();
                var options = serviceProvider.GetRequiredService<IOptions<WorkflowForgeOptions>>().Value;

                // Pass options directly to WorkflowSmith - no mapping needed!
                return WorkflowForge.CreateSmith(logger, serviceProvider, options);
            });

            return services;
        }
    }
}