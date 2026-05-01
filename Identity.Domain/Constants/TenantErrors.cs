using Identity.Domain.Entities;
using Shared.Domain.Primitives;

namespace Identity.Domain.Constants;

/// <summary>
/// User domain errors
/// </summary>
public static class TenantErrors
{
    public static readonly Error NotFound = new(
        "Tenant.NotFound",
        "Tenant not found");

    public static readonly Error SubdomainExists = new(
        "Tenant.SubdomainExists",
        "Subdomain already exists");

    public static readonly Error NameRequired = new(
        "Tenant.NameRequired", 
        "Tenant name is required");

    public static readonly Error SubdomainRequired = new(
        "Tenant.SubdomainRequired",
        "Subdomain is required");

    public static readonly Error EmailRequired = new(
        "Tenant.EmailRequired",
        "Contact email is required");

    public static readonly Error HasActiveUsers = new(
        "Tenant.HasActiveUsers",
        "Cannot delete a tenant that still has active users");
}
