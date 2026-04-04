using Identity.Application.Dtos.Tenants;
using Identity.Application.Models.Tenant;
using Identity.Domain.Entities;
using Shared.Domain.Enums;

namespace Identity.Application.Mappings.Tenants
{
    public static class TenantMappings
    {
        public static List<TenantDto> ToListDtos(this IEnumerable<Tenant> tenants)
        {
            return tenants.Select(t => t.ToDto()).ToList();
        }

        public static TenantDto ToDto(this Tenant tenant)
        {
            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Subdomain = tenant.Subdomain,
                ContactEmail = tenant.ContactEmail,
                Status = (EntityStatusEnum)tenant.StatusId,
                SubscriptionPlan = tenant.SubscriptionPlan,
                MaxUsers = tenant.MaxUsers,
                CreatedOn = tenant.CreatedOn.UtcDateTime
            };
        }

        public static Tenant ToEntity(this TenantModel model)
        {
            return new Tenant
            {
                Name = model.Name,
                Subdomain = model.Subdomain,
                ContactEmail = model.ContactEmail,
                StatusId = (short)EntityStatusEnum.Active,
                SubscriptionPlan = "Free",
                MaxUsers = 10,
                CreatedOn = DateTimeOffset.UtcNow,
                CreatedByName = "Admin",
                CreatedByEmail = "admin@TKDHub.com",
            };
        }
    }
}
