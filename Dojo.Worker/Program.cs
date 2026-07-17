using Dojo.Infrastructure;
using Dojo.Worker.BackgroundServices;
using Shared.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// Same infra wiring Dojo.API uses (DbContext, repositories, WhatsApp, Identity client) —
// AddHttpContextAccessor() registers real ITenantContext/IBranchContext that resolve to
// "no tenant" here since a worker never has an ambient HttpContext, which is exactly what
// the expiry sweep's cross-tenant query relies on.
builder.Services.AddSharedInfrastructure();
builder.Services.AddDojoInfrastructure(builder.Configuration);

// The one thing Dojo.API deliberately does NOT register — see AddStudentExpirySweep's
// summary for why this must run as its own single-instance process, not per API replica.
builder.Services.AddStudentExpirySweep();
builder.Services.AddHostedService<StudentExpiryBackgroundService>();

var host = builder.Build();
host.Run();
