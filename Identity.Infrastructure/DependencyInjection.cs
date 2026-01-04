using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Repositories;
using Shared.Infrastructure.Authentication;
using Shared.Infrastructure.Repositories;

namespace Identity.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("IdentityDatabase"),
                    b => b.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName)));

            services.AddScoped<ITenantRepository, TenantRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IErrorLogRepository>(provider =>
            {
                var dbContext = provider.GetRequiredService<IdentityDbContext>();
                return new ErrorLogRepository(dbContext);
            });
            
            // JWT Settings
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

            return services;
        }
    }
}
