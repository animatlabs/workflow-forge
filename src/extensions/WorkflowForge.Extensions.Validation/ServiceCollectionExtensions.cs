using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowForge.Extensions.Validation.Options;

namespace WorkflowForge.Extensions.Validation
{
    /// <summary>
    /// Extension methods for configuring Validation middleware in dependency injection containers.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures Validation middleware options from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection to add configuration to.</param>
        /// <param name="configuration">The configuration instance containing validation settings.</param>
        /// <param name="sectionName">The configuration section name. Defaults to <see cref="ValidationMiddlewareOptions.DefaultSectionName"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
        public static IServiceCollection AddValidationConfiguration(
            this IServiceCollection services,
            IConfiguration configuration,
            string? sectionName = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            sectionName ??= ValidationMiddlewareOptions.DefaultSectionName;
            services.Configure<ValidationMiddlewareOptions>(configuration.GetSection(sectionName));
            return services;
        }
    }
}
