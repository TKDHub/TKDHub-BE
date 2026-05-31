using Microsoft.AspNetCore.Http;
using Shared.Application.Contracts;
using Shared.Domain.Enums;
using System.Security.Claims;

namespace Shared.Infrastructure.MultiTenancy;

public sealed class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public bool IsSuperAdmin
        => _httpContextAccessor.HttpContext?.User
               .FindAll(ClaimTypes.Role)
               .Any(c => c.Value == nameof(UserRoleEnum.SuberAdmin))
           ?? false;
}
