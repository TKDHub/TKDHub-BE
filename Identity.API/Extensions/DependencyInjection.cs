using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Identity.Application.Commands.Authentications;
using Identity.Application.Contracts;
using Identity.Application.Services;
using Identity.Application.Validators.Tenant;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Application.Behaviors;
using Shared.Infrastructure;
using Shared.Infrastructure.Authentication;
using Shared.Infrastructure.Extensions;

namespace Identity.API.Extensions
{
    /// <summary>
    /// Extension methods for service registration
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Configure the HTTP request pipeline
        /// </summary>
        public static IApplicationBuilder UseIdentityApiMiddleware(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.ApplyMigrations();

            app.UseGlobalExceptionHandler();

            // Swagger (only in Development)
            if (env.IsDevelopment())
            {
                app.UseSwaggerDocumentation();
            }

            // Security headers (recommended for production)
            app.UseSecurityHeaders();

            // HTTPS redirection
            app.UseHttpsRedirection();

            // CORS
            app.UseCors();

            // Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }

        /// <summary>
        /// Add security headers middleware
        /// </summary>
        private static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                // Add security headers
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Referrer-Policy", "no-referrer");

                await next();
            });

            return app;
        }

        /// <summary>
        /// Add all Identity API services
        /// </summary>
        public static IServiceCollection AddIdentityApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add Controllers
            services.AddControllers();
            services.AddEndpointsApiExplorer();

            // Add JWT Authentication
            services.AddJwtAuthentication(configuration);

            // Add MediatR
            services.AddMediatRServices();

            // Add FluentValidation
            services.AddFluentValidationServices();

            // Add Infrastructure (DbContext, Repositories, etc.)
            services.AddIdentityInfrastructure(configuration);

            // Add Shared Infrastructure (TenantContext)
            services.AddSharedInfrastructure();

            services.AddDomainServices();

            // Add CORS
            services.AddCorsPolicy();

            return services;
        }

        /// <summary>
        /// Configure JWT Authentication
        /// </summary>
        private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration
                .GetSection(JwtSettings.SectionName)
                .Get<JwtSettings>();

            if (jwtSettings == null)
            {
                throw new InvalidOperationException("JWT settings are not configured");
            }

            services.Configure<JwtSettings>(
                configuration.GetSection(JwtSettings.SectionName));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddAuthorization();

            return services;
        }

        /// <summary>
        /// Configure MediatR with behaviors
        /// </summary>
        private static IServiceCollection AddMediatRServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            });

            return services;
        }

        /// <summary>
        /// Configure FluentValidation
        /// </summary>
        private static IServiceCollection AddFluentValidationServices(this IServiceCollection services)
        {
            services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
            services.AddValidatorsFromAssembly(typeof(TenantModelValidator).Assembly); //this Registers ALL validators in the same assembly

            return services;
        }

        /// <summary>
        /// Configure CORS policy
        /// </summary>
        private static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            return services;
        }

        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IAuthenticationService, AuthenticationService>();

            return services;
        }

        public static IApplicationBuilder ApplyMigrations(this IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

                // Checks if database exists and applies only pending migrations
                db.Database.Migrate();
            }

            return app;
        }
    }
}
