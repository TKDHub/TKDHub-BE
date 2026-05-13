using Microsoft.AspNetCore.Http;
using Shared.Application.Contracts;

namespace Shared.Infrastructure.MultiTenancy
{
    public sealed class BranchContext : IBranchContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BranchContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid BranchId
        {
            get
            {
                var headerValue = _httpContextAccessor.HttpContext?
                    .Request.Headers["X-Branch-Id"].FirstOrDefault();

                return Guid.TryParse(headerValue, out var branchId) ? branchId : Guid.Empty;
            }
        }
    }
}
