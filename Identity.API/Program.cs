using Identity.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add all services in one line
builder.Services.AddIdentityApiServices(builder.Configuration);

// ✅ Add Swagger documentation
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// ✅ Configure middleware pipeline
app.UseIdentityApiMiddleware(app.Environment);

// ✅ Map root redirect
app.MapRootRedirect();

// ✅ Map controllers
app.MapControllers();

app.Run();