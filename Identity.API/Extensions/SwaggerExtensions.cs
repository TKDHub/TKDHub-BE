using Microsoft.OpenApi.Models;

namespace Identity.API.Extensions
{
    /// <summary>
    /// Swagger configuration extensions
    /// </summary>
    public static class SwaggerExtensions
    {
        /// <summary>
        /// Add Swagger with JWT configuration
        /// </summary>
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Identity API",
                    Version = "v1",
                    Description = "Identity Microservice",
                    Contact = new OpenApiContact
                    {
                        Name = "Abdullah Abusubaih",
                        Email = "abdullahabusubaih7@gmail.com"
                    }
                });

                // JWT Bearer authentication
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
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
                               Id = "Bearer"
                           }
                       },
                       Array.Empty<string>()
                   }
                });
            });

            return services;
        }

        /// <summary>
        /// Configure Swagger UI with custom route
        /// </summary>
        public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
        {
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "Identity/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "Identity";
                c.SwaggerEndpoint("/Identity/v1/swagger.json", "Identity API v1");
                c.DocumentTitle = "Identity API Documentation";

                // Optional: Customize UI
                c.DisplayRequestDuration();
                c.EnableDeepLinking();
                c.EnableFilter();
            });

            return app;
        }

        /// <summary>
        /// Map root redirect to Swagger
        /// </summary>
        public static WebApplication MapRootRedirect(this WebApplication app)
        {
            app.MapGet("/", context =>
            {
                context.Response.Redirect("/Identity/index.html");
                return Task.CompletedTask;
            });

            return app;
        }
    }
}
