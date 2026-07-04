using Dojo.Domain.Repositories;
using Dojo.Infrastructure.Persistence;
using Dojo.Infrastructure.Persistence.Repositories;
using Dojo.Infrastructure.Services;
using Dojo.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Contracts;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.Repositories;

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
            services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
            services.AddScoped<IIncomeInvoiceRepository, IncomeInvoiceRepository>();
            services.AddScoped<IOutcomeInvoiceRepository, OutcomeInvoiceRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork<DojoDbContext>>();

            // Identity service clients
            services.Configure<IdentityApiSettings>(configuration.GetSection(IdentityApiSettings.SectionName));
            services.AddHttpClient("IdentityApi");
            services.AddScoped<IBranchService, IdentityBranchService>();

            // Cloudflare Images
            services.AddCloudinaryImages(configuration);

            // Centralised error logging (HTTP → Identity)
            services.AddHttpErrorLogService(configuration);

            return services;
        }
    }
}
