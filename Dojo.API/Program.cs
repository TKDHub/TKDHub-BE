using Dojo.API.Extensions;
using Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDojoApiServices(builder.Configuration);
builder.Services.AddSwaggerDocumentation(ApiMetadata.ApiInfo);

var app = builder.Build();

app.UseDojoApiMiddleware(app.Environment, ApiMetadata.ApiInfo);
app.MapRootRedirect(ApiMetadata.ApiInfo);
app.MapGet("/health", () => Results.Ok("healthy"));
app.MapControllers();

app.Run();
