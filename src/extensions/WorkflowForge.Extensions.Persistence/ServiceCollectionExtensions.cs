using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace WorkflowForge.Extensions.Persistence
{
    /// <summary>
    /// Extension methods for configuring Persistence middleware in dependency injection containers.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures Persistence middleware options from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection to add configuration to.</param>
        /// <param name="configuration">The configuration instance containing persistence settings.</param>
        /// <param name="sectionName">The configuration section name. Defaults to <see cref="PersistenceOptions.DefaultSectionName"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
        public static IServiceCollection AddPersistenceConfiguration(
            this IServiceCollection services,
            IConfiguration configuration,
            string? sectionName = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            sectionName ??= PersistenceOptions.DefaultSectionName;
            services.Configure<PersistenceOptions>(configuration.GetSection(sectionName));
            return services;
        }
    }
}