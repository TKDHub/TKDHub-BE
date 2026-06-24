using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Identity.Application.Commands.Authentications;
using Identity.Application.Contracts;
using Identity.Application.Services;
using Identity.Application.Validators.Tenant;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Identity.API.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure;
using Shared.Infrastructure.Extensions;

namespace Identity.API.Extensions
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers all Identity API services.
        /// </summary>
        public static IServiceCollection AddIdentityApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                    // Accept & emit enum names (e.g. "Equals", "Admin") in JSON bodies.
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            services.AddEndpointsApiExplorer();

            services.AddJwtAuthentication(configuration);
            services.AddMediatRWithBehaviors(typeof(RegisterCommand).Assembly);
            services.AddFluentValidationFromAssembly(typeof(TenantModelValidator).Assembly);

            services.AddIdentityInfrastructure(configuration);
            services.AddSharedInfrastructure();
            services.AddDomainServices();

            services.Configure<RestKeySettings>(configuration.GetSection(RestKeySettings.SectionName));

            services.AddCorsPolicy(configuration);
            services.AddRateLimiting();

            return services;
        }

        /// <summary>
        /// Configures the Identity middleware pipeline.
        /// </summary>
        public static IApplicationBuilder UseIdentityApiMiddleware(this IApplicationBuilder app, IWebHostEnvironment env, ApiInfo info)
        {
            app.ApplyMigrations<IdentityDbContext>();
            app.UseSharedApiMiddleware(env, info);
            app.UseRateLimiter();

            return app;
        }

        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            return services;
        }

        private static IServiceCollection AddRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.AddPolicy("AuthPolicy", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            return services;
        }
    }
}
