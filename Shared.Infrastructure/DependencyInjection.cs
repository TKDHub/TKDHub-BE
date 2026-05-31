using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Contracts;
using Shared.Infrastructure.MultiTenancy;

namespace Shared.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
        {
            // Required for tenant/branch resolution
            services.AddHttpContextAccessor();

            // Register tenant context
            services.AddScoped<ITenantContext, TenantContext>();

            // Register branch context
            services.AddScoped<IBranchContext, BranchContext>();

            // Register user context
            services.AddScoped<IUserContext, UserContext>();

            return services;
        }
    }
}
