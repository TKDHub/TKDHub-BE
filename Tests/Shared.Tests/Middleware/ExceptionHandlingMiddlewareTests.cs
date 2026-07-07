using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shared.Application.Contracts;
using Shared.Application.Models;
using Shared.Domain.Exceptions;
using Shared.Infrastructure.Middleware;

namespace Shared.Tests.Middleware;

/// <summary>
/// Drives ExceptionHandlingMiddleware through a real ASP.NET Core request pipeline
/// (Microsoft.AspNetCore.TestHost) instead of calling it as a plain C# method — proving the
/// exception-to-HTTP-response mapping actually works end-to-end, without booting either real
/// API host (which would require a live database/external HTTP dependencies).
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(HttpResponseMessage Response, IErrorLogService ErrorLogService)> SendAsync(
        Exception exceptionToThrow, string environment = "Production", string path = "/api/boom")
    {
        var errorLogService = Substitute.For<IErrorLogService>();

        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .UseEnvironment(environment)
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(errorLogService);
                        services.AddLogging();
                    })
                    .Configure(app =>
                    {
                        app.UseMiddleware<ExceptionHandlingMiddleware>();
                        app.Run(_ => throw exceptionToThrow);
                    });
            })
            .StartAsync();

        var response = await host.GetTestClient().GetAsync(path);
        return (response, errorLogService);
    }

    [Fact]
    public async Task NotFoundException_MapsTo404WithProblemDetailsBody()
    {
        var (response, _) = await SendAsync(new NotFoundException("Student", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("Resource Not Found", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ValidationException_MapsTo400WithErrorsDictionary()
    {
        var (response, _) = await SendAsync(new ValidationException("Name", "Name is required"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Validation Error", body.GetProperty("title").GetString());
        Assert.True(body.GetProperty("errors").TryGetProperty("Name", out _));
    }

    [Fact]
    public async Task UnauthorizedException_MapsTo401()
    {
        var (response, _) = await SendAsync(new UnauthorizedException("Not allowed"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ForbiddenException_MapsTo403()
    {
        var (response, _) = await SendAsync(new ForbiddenException("No access"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnhandledException_MapsTo500_AndHidesMessageInProduction()
    {
        var (response, _) = await SendAsync(new InvalidOperationException("secret internal detail"), environment: "Production");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var detail = body.GetProperty("detail").GetString();
        Assert.DoesNotContain("secret internal detail", detail);
    }

    [Fact]
    public async Task UnhandledException_ExposesMessageInDevelopment()
    {
        var (response, _) = await SendAsync(new InvalidOperationException("dev-visible detail"), environment: "Development");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("dev-visible detail", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task AnyException_ForwardsPayloadToErrorLogService()
    {
        var (response, errorLogService) = await SendAsync(new NotFoundException("Student", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await errorLogService.Received(1).LogAsync(
            Arg.Is<ErrorLogPayload>(p => p.ExceptionType == nameof(NotFoundException) && p.StatusCode == 404),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HealthCheckPath_DoesNotForwardErrorLog()
    {
        var (response, errorLogService) = await SendAsync(new InvalidOperationException("boom"), path: "/health");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await errorLogService.DidNotReceive().LogAsync(Arg.Any<ErrorLogPayload>(), Arg.Any<CancellationToken>());
    }
}
