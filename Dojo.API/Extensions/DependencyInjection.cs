using System.Text.Json.Serialization;
using Dojo.Application.Commands.Students;
using Dojo.Infrastructure;
using Dojo.Infrastructure.Persistence;
using Dojo.API.Settings;
using Shared.Infrastructure;
using Shared.Infrastructure.Extensions;

namespace Dojo.API.Extensions
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers all Dojo API services.
        /// </summary>
        public static IServiceCollection AddDojoApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                    // Accept & emit enum names (e.g. "Subscription", "Visa", "Equals") in JSON bodies.
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            services.AddEndpointsApiExplorer();

            services.AddJwtAuthentication(configuration);
            services.AddMediatRWithBehaviors(typeof(CreateStudentCommand).Assembly);
            services.AddFluentValidationFromAssembly(typeof(CreateStudentCommand).Assembly);

            services.AddDojoInfrastructure(configuration);
            services.AddSharedInfrastructure();

            services.Configure<RestKeySettings>(configuration.GetSection(RestKeySettings.SectionName));

            services.AddCorsPolicy(configuration);

            return services;
        }

        /// <summary>
        /// Configures the Dojo middleware pipeline.
        /// </summary>
        public static IApplicationBuilder UseDojoApiMiddleware(this IApplicationBuilder app, IWebHostEnvironment env, ApiInfo info)
        {
            app.ApplyMigrations<DojoDbContext>();
            app.UseSharedApiMiddleware(env, info);

            return app;
        }
    }
}
