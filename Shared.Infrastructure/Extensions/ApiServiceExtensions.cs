using System.Reflection;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Shared.Application.Behaviors;
using Shared.Application.Contracts;
using Shared.Infrastructure.Authentication;
using Shared.Infrastructure.Services;
using Shared.Infrastructure.Settings;

namespace Shared.Infrastructure.Extensions
{
    public static class ApiServiceExtensions
    {
        /// <summary>
        /// Registers <see cref="CloudinaryImageService"/> as the <see cref="IImageService"/>
        /// implementation. Configure <c>CloudinarySettings</c> in appsettings with
        /// <c>CloudName</c>, <c>ApiKey</c>, and <c>ApiSecret</c>.
        /// </summary>
        public static IServiceCollection AddCloudinaryImages(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CloudinarySettings>(
                configuration.GetSection(CloudinarySettings.SectionName));
            services.AddScoped<IImageService, CloudinaryImageService>();
            return services;
        }

        /// <summary>
        /// Registers <see cref="HttpErrorLogService"/> as the <see cref="IErrorLogService"/>
        /// implementation for all services. Configure <c>ErrorLogSettings</c> in appsettings
        /// with the Identity base URL and shared service key.
        /// </summary>
        public static IServiceCollection AddHttpErrorLogService(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ErrorLogSettings>(
                configuration.GetSection(ErrorLogSettings.SectionName));
            services.AddHttpClient("ErrorLogClient");
            services.AddScoped<IErrorLogService, HttpErrorLogService>();
            return services;
        }

        /// <summary>
        /// Registers JWT Bearer authentication using <c>JwtSettings</c> from configuration.
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration
                .GetSection(JwtSettings.SectionName)
                .Get<JwtSettings>()
                ?? throw new InvalidOperationException("JWT settings are not configured.");

            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = true,
                        ValidateAudience         = true,
                        ValidateLifetime         = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer              = jwtSettings.Issuer,
                        ValidAudience            = jwtSettings.Audience,
                        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                        ClockSkew                = TimeSpan.Zero
                    };
                });

            services.AddAuthorization();

            return services;
        }

        /// <summary>
        /// Registers MediatR from the given assembly, with <see cref="ValidationBehavior{TRequest,TResponse}"/>
        /// and <see cref="LoggingBehavior{TRequest,TResponse}"/> pipeline behaviors.
        /// </summary>
        public static IServiceCollection AddMediatRWithBehaviors(this IServiceCollection services, Assembly commandAssembly)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(commandAssembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            });

            return services;
        }

        /// <summary>
        /// Registers all FluentValidation validators found in the given assembly.
        /// </summary>
        public static IServiceCollection AddFluentValidationFromAssembly(this IServiceCollection services, Assembly validatorAssembly)
        {
            services.AddFluentValidationAutoValidation()
                    .AddFluentValidationClientsideAdapters();

            services.AddValidatorsFromAssembly(validatorAssembly);

            return services;
        }

        /// <summary>
        /// Configures CORS using <c>AllowedOrigins</c> from configuration.
        /// Falls back to <c>localhost:3000</c> if the section is absent.
        /// </summary>
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();

            if (allowedOrigins == null || allowedOrigins.Length == 0)
                allowedOrigins = ["http://localhost:3000", "https://localhost:3000"];

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            return services;
        }

        /// <summary>
        /// Applies any pending EF Core migrations for <typeparamref name="TDbContext"/> at startup.
        /// </summary>
        public static IApplicationBuilder ApplyMigrations<TDbContext>(this IApplicationBuilder app)
            where TDbContext : DbContext
        {
            using var scope = app.ApplicationServices.CreateScope();
            scope.ServiceProvider.GetRequiredService<TDbContext>().Database.Migrate();
            return app;
        }

        /// <summary>
        /// Configures the shared middleware pipeline: exception handling, Swagger (dev only),
        /// security headers, HTTPS redirection, CORS, and authentication/authorization.
        /// </summary>
        public static IApplicationBuilder UseSharedApiMiddleware(this IApplicationBuilder app, IWebHostEnvironment env, ApiInfo info)
        {
            app.UseGlobalExceptionHandler();

            if (env.IsDevelopment())
                app.UseSwaggerDocumentation(info);

            app.UseSecurityHeaders();
            app.UseHttpsRedirection();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }

        /// <summary>
        /// Adds standard security response headers (CSP-lite, no-sniff, no-frame, XSS protection).
        /// </summary>
        private static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Referrer-Policy", "no-referrer");
                await next();
            });

            return app;
        }
    }
}
