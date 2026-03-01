using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Extensions.Persistence.Recovery.Options;

namespace WorkflowForge.Extensions.Persistence.Recovery
{
    /// <summary>
    /// Extension methods for configuring workflow recovery services on an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="RecoveryMiddlewareOptions"/> from configuration, binding to the specified (or default) section name.
        /// </summary>
        /// <param name="services">The service collection to add the configuration to.</param>
        /// <param name="configuration">The application configuration root.</param>
        /// <param name="sectionName">Optional configuration section name; defaults to <see cref="RecoveryMiddlewareOptions.DefaultSectionName"/>.</param>
        /// <returns>The <paramref name="services"/> instance for chaining.</returns>
        public static IServiceCollection AddRecoveryConfiguration(
            this IServiceCollection services,
            IConfiguration configuration,
            string? sectionName = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            sectionName ??= RecoveryMiddlewareOptions.DefaultSectionName;
            services.Configure<RecoveryMiddlewareOptions>(configuration.GetSection(sectionName));
            return services;
        }
    }
}
