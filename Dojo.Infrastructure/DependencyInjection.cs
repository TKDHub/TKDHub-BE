using Dojo.Domain.Repositories;
using Dojo.Infrastructure.Persistence;
using Dojo.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Repositories;
using Shared.Infrastructure.Persistence.Repositories;
using Shared.Infrastructure.Repositories;

namespace Dojo.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDojoInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<DojoDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DojoDatabase"),
                    b => b.MigrationsAssembly(typeof(DojoDbContext).Assembly.FullName)));

            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork<DojoDbContext>>();
            services.AddScoped<IErrorLogRepository>(provider =>
            {
                var dbContext = provider.GetRequiredService<DojoDbContext>();
                return new ErrorLogRepository(dbContext);
            });

            return services;
        }
    }
}
