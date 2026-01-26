using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using WorkflowForge.Extensions.Persistence.Recovery.Options;

namespace WorkflowForge.Extensions.Persistence.Recovery
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRecoveryConfiguration(
            this IServiceCollection services,
            IConfiguration configuration,
            string? sectionName = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            sectionName ??= RecoveryMiddlewareOptions.DefaultSectionName;
            services.Configure<RecoveryMiddlewareOptions>(configuration.GetSection(sectionName));
            return services;
        }
    }
}