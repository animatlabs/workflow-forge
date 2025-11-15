using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using WorkflowForge.Extensions.Audit.Options;

namespace WorkflowForge.Extensions.Audit
{
    /// <summary>
    /// Extension methods for configuring Audit middleware in dependency injection containers.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures Audit middleware options from the specified configuration section.
        /// </summary>
        /// <param name="services">The service collection to add configuration to.</param>
        /// <param name="configuration">The configuration instance containing audit settings.</param>
        /// <param name="sectionName">The configuration section name. Defaults to <see cref="AuditMiddlewareOptions.DefaultSectionName"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
        public static IServiceCollection AddAuditConfiguration(
            this IServiceCollection services,
            IConfiguration configuration,
            string? sectionName = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            sectionName ??= AuditMiddlewareOptions.DefaultSectionName;
            services.Configure<AuditMiddlewareOptions>(configuration.GetSection(sectionName));
            return services;
        }
    }
}
