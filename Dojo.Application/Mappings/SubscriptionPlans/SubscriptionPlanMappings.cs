using Dojo.Application.Dtos.Students;
using Dojo.Application.Dtos.SubscriptionPlans;
using Dojo.Application.Mappings.Students;
using Dojo.Application.Models.SubscriptionPlan;
using Dojo.Domain.Entities;
using Shared.Domain.Enums;

namespace Dojo.Application.Mappings.SubscriptionPlans;

public static class SubscriptionPlanMappings
{
    public static List<SubscriptionPlanDto> ToListDtos(this IEnumerable<SubscriptionPlan> plans, string currency)
        => plans.Select(p => p.ToDto(currency)).ToList();

    public static SubscriptionPlanDto ToDto(this SubscriptionPlan plan, string currency, List<StudentDto>? students = null)
        => new()
        {
            Id             = plan.Id,
            TenantId       = plan.TenantId,
            BranchId       = plan.BranchId,
            Name           = plan.Name,
            Description    = plan.Description,
            DurationMonths = plan.DurationMonths,
            Price          = plan.Price,
            Currency       = currency,
            Status         = ((EntityStatusEnum)plan.StatusId).ToString(),
            CreatedOn      = plan.CreatedOn,
            ModifiedOn     = plan.ModifiedOn,
            Students       = students
        };

    public static SubscriptionPlan ToEntity(this SubscriptionPlanModel model, Guid branchId, Guid tenantId)
        => new()
        {
            BranchId       = branchId,
            TenantId       = tenantId,
            Name           = model.Name.Trim(),
            Description    = model.Description?.Trim(),
            DurationMonths = model.DurationMonths,
            Price          = model.Price,
            StatusId       = (short)EntityStatusEnum.Active,
            CreatedOn      = DateTimeOffset.UtcNow,
            CreatedByEmail = model.CreatedByEmail,
            CreatedByName  = model.CreatedByName
        };

    public static SubscriptionPlan ApplyUpdate(this SubscriptionPlan plan, SubscriptionPlanModel model)
    {
        plan.Name           = model.Name.Trim();
        plan.Description    = model.Description?.Trim();
        plan.DurationMonths = model.DurationMonths;
        plan.Price          = model.Price;
        plan.ModifiedOn     = DateTimeOffset.UtcNow;
        plan.ModifiedByEmail = model.ModifiedByEmail;
        plan.ModifiedByName  = model.ModifiedByName;
        return plan;
    }
}
