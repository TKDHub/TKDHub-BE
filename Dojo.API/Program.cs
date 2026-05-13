using Dojo.API.Extensions;
using Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDojoApiServices(builder.Configuration);
builder.Services.AddSwaggerDocumentation(ApiMetadata.ApiInfo);

var app = builder.Build();

app.UseDojoApiMiddleware(app.Environment, ApiMetadata.ApiInfo);
app.MapRootRedirect(ApiMetadata.ApiInfo);
app.MapGet("/health", () => Results.Ok("healthy"));

// ── Dev-only exception-handler smoke test ─────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/test/throw", () =>
    {
        throw new InvalidOperationException("Intentional test exception — verifying Dojo→Identity error-log pipeline.");
    }).WithTags("Test").WithSummary("Throws an unhandled exception to smoke-test the error log pipeline.");
}
// ─────────────────────────────────────────────────────────────────────────────

app.MapControllers();

app.Run();
