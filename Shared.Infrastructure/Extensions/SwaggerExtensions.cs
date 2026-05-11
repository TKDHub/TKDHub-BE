using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Shared.Infrastructure.Extensions
{
    public static class SwaggerExtensions
    {
        /// <summary>
        /// Registers Swagger with JWT Bearer auth, using service-specific <see cref="ApiInfo"/>.
        /// </summary>
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, ApiInfo info)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(info.Version, new OpenApiInfo
                {
                    Title       = info.Title,
                    Version     = info.Version,
                    Description = info.Description,
                    Contact     = new OpenApiContact
                    {
                        Name  = "Abdullah Abusubaih",
                        Email = "abdullahabusubaih7@gmail.com"
                    }
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name        = "Authorization",
                    Type        = SecuritySchemeType.ApiKey,
                    Scheme      = "Bearer",
                    BearerFormat = "JWT",
                    In          = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your token"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            UnresolvedReference = true,
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id   = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }

        /// <summary>
        /// Enables Swagger UI at <c>/{routePrefix}</c> using the service-specific route prefix.
        /// </summary>
        public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app, ApiInfo info)
        {
            app.UseSwagger(c =>
            {
                c.RouteTemplate = $"{info.RoutePrefix}/{{documentName}}/swagger.json";
            });

            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix   = info.RoutePrefix;
                c.SwaggerEndpoint($"/{info.RoutePrefix}/{info.Version}/swagger.json", $"{info.Title} {info.Version}");
                c.DocumentTitle = $"{info.Title} Documentation";
                c.DisplayRequestDuration();
                c.EnableDeepLinking();
                c.EnableFilter();
            });

            return app;
        }

        /// <summary>
        /// Maps <c>GET /</c> to redirect to the Swagger UI for this service.
        /// </summary>
        public static WebApplication MapRootRedirect(this WebApplication app, ApiInfo info)
        {
            app.MapGet("/", context =>
            {
                context.Response.Redirect($"/{info.RoutePrefix}/index.html");
                return Task.CompletedTask;
            });

            return app;
        }
    }
}
