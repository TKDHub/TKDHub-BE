using Identity.Application.Dtos.Branches;
using Identity.Application.Models.Branch;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Shared.Domain.Enums;

namespace Identity.Application.Mappings.Branches;

public static class BranchMappings
{
    public static List<BranchDto> ToListDtos(this IEnumerable<Branch> branches)
        => branches.Select(b => b.ToDto()).ToList();

    public static BranchDto ToDto(this Branch branch)
    {
        return new BranchDto
        {
            Id = branch.Id,
            TenantId = branch.TenantId,
            Name = branch.Name,
            Email = branch.Email,
            PhoneNumber = branch.PhoneNumber,
            Enabled = branch.Enabled,
            AddressCountry = branch.AddressCountry,
            AddressState = branch.AddressState,
            AddressCity = branch.AddressCity,
            AddressStreet = branch.AddressStreet,
            TimeZone = branch.TimeZone,
            Currency = branch.Currency?.ToString()
        };
    }

    public static Branch ApplyUpdate(this Branch branch, BranchModel model)
    {
        branch.Name = model.Name.Trim();
        branch.Email = model.Email.Trim().ToLowerInvariant();
        branch.PhoneNumber = model.PhoneNumber?.Trim();
        branch.Enabled = model.Enabled;
        branch.AddressCountry = model.AddressCountry?.Trim();
        branch.AddressState = model.AddressState?.Trim();
        branch.AddressCity = model.AddressCity?.Trim();
        branch.AddressStreet = model.AddressStreet?.Trim();
        branch.TimeZone = model.TimeZone;
        branch.Currency = Enum.TryParse<CurrencyEnum>(model.Currency, ignoreCase: true, out var currency) ? currency : null;
        branch.ModifiedOn = DateTimeOffset.UtcNow;
        branch.ModifiedByEmail = model.ModifiedByEmail;
        branch.ModifiedByName = model.ModifiedByName;
        return branch;
    }

    public static Branch ToEntity(this BranchModel model)
    {
        return new Branch
        {
            TenantId = model.TenantId,
            Name = model.Name.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            PhoneNumber = model.PhoneNumber?.Trim(),
            Enabled = model.Enabled,
            AddressCountry = model.AddressCountry?.Trim(),
            AddressState = model.AddressState?.Trim(),
            AddressCity = model.AddressCity?.Trim(),
            AddressStreet = model.AddressStreet?.Trim(),
            TimeZone = model.TimeZone,
            Currency = Enum.TryParse<CurrencyEnum>(model.Currency, ignoreCase: true, out var currency) ? currency : null,
            StatusId = (short)EntityStatusEnum.Active,
            CreatedOn = DateTimeOffset.UtcNow,
            CreatedByEmail = model.CreatedByEmail,
            CreatedByName = model.CreatedByName
        };
    }
}
