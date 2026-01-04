using Microsoft.AspNetCore.Http;
using Shared.Application.Contracts;

namespace Shared.Infrastructure.MultiTenancy
{
    public sealed class TenantContext : ITenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid TenantId
        {
            get
            {
                // Extract from JWT token
                var tenantIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("TenantId")?.Value;

                if (Guid.TryParse(tenantIdClaim, out var tenantId))
                {
                    return tenantId;
                }

                // Fallback: Check header (for service-to-service calls)
                var headerTenantId = _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();

                if (Guid.TryParse(headerTenantId, out var headerTenant))
                {
                    return headerTenant;
                }

                return Guid.Empty;
            }
        }

        public string TenantName => _httpContextAccessor.HttpContext?.User.FindFirst("TenantName")?.Value ?? "Unknown";

        public bool IsMultiTenant => TenantId != Guid.Empty;
    }
}