using Dojo.Application.Services;
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
            services.AddScoped<IClassRepository, ClassRepository>();
            services.AddScoped<IStudentActivityLogRepository, StudentActivityLogRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork<DojoDbContext>>();

            // Identity service clients
            services.Configure<IdentityApiSettings>(configuration.GetSection(IdentityApiSettings.SectionName));
            services.AddHttpClient("IdentityApi");
            services.AddScoped<IBranchService, IdentityBranchService>();
            services.AddScoped<INotificationTargetsService, IdentityNotificationTargetsService>();

            // Cloudflare Images
            services.AddCloudinaryImages(configuration);

            // Centralised error logging (HTTP → Identity)
            services.AddHttpErrorLogService(configuration);

            // WhatsApp notifications (Meta Cloud API)
            services.AddWhatsAppNotifications(configuration);

            return services;
        }

        /// <summary>
        /// Registers <see cref="IStudentExpiryProcessor"/> — the testable business logic behind
        /// the daily student-expiry sweep. The hosted service that actually schedules and runs
        /// it (StudentExpiryBackgroundService) lives in, and is registered by, the standalone
        /// Dojo.Worker process — never Dojo.API. Dojo.API is horizontally scaled, and a hosted
        /// service registered there would run once per replica, sweeping the same students and
        /// sending duplicate WhatsApp notifications. Dojo.Worker runs as a single instance
        /// specifically so this only ever fires once per day.
        /// </summary>
        public static IServiceCollection AddStudentExpirySweep(this IServiceCollection services)
        {
            services.AddScoped<IStudentExpiryProcessor, StudentExpiryProcessor>();
            return services;
        }
    }
}
