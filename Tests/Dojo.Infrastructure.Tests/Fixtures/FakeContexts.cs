using Microsoft.AspNetCore.Http;
using Shared.Application.Contracts;

namespace Dojo.Infrastructure.Tests.Fixtures;

internal sealed class FakeTenantContext(Guid tenantId) : ITenantContext
{
    public Guid TenantId { get; } = tenantId;
    public string TenantName { get; } = "Test Tenant";
    public bool IsMultiTenant { get; } = true;
}

internal sealed class FakeBranchContext(Guid branchId) : IBranchContext
{
    public Guid BranchId { get; } = branchId;
}

internal sealed class FakeHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }
}
