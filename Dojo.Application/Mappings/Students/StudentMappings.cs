using Dojo.Application.Dtos.Students;
using Dojo.Application.Models.Student;
using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Shared.Domain.Enums;

namespace Dojo.Application.Mappings.Students;

public static class StudentMappings
{
    public static List<StudentDto> ToListDtos(this IEnumerable<Student> students)
        => students.Select(s => s.ToDto()).ToList();

    public static StudentDto ToDto(this Student student)
        => new()
        {
            Id                   = student.Id,
            TenantId             = student.TenantId,
            BranchId             = student.BranchId,
            FirstName            = student.FirstName,
            LastName             = student.LastName,
            FullName             = student.FullName,
            Email                = student.Email,
            PhoneNumber          = student.PhoneNumber,
            DateOfBirth          = student.DateOfBirth,
            Gender               = student.Gender.ToString(),
            StartDate            = student.StartDate,
            BeltLevel            = student.BeltLevel.ToString(),
            SubscriptionPlanId   = student.SubscriptionPlanId,
            SubscriptionPlanName = student.SubscriptionPlan?.Name ?? string.Empty,
            Price                = student.Price,
            Currency             = student.Currency,
            DurationMonths       = student.DurationMonths,
            ProfileImageUrl      = student.ProfileImageUrl,
            EmergencyContact     = student.EmergencyContact,
            Status               = ((StudentStatusEnum)student.StatusId).ToString()
        };

    public static Student ToEntity(this StudentModel model, Guid branchId, Guid tenantId)
        => new()
        {
            BranchId           = branchId,
            TenantId           = tenantId,
            FirstName          = model.FirstName.Trim(),
            LastName           = model.LastName.Trim(),
            Email              = model.Email?.Trim().ToLowerInvariant(),
            PhoneNumber        = model.PhoneNumber.Trim(),
            DateOfBirth        = model.DateOfBirth,
            Gender             = Enum.TryParse<GenderEnum>(model.Gender, ignoreCase: true, out var gender) ? gender : GenderEnum.Other,
            StartDate          = model.StartDate,
            BeltLevel          = Enum.TryParse<BeltLevelEnum>(model.BeltLevel, ignoreCase: true, out var belt) ? belt : BeltLevelEnum.White,
            SubscriptionPlanId = model.SubscriptionPlanId,
            Price              = model.Price,
            Currency           = model.Currency,
            DurationMonths     = model.DurationMonths,
            ProfileImageUrl    = model.ProfileImageUrl?.Trim(),
            EmergencyContact   = model.EmergencyContact?.Trim(),
            StatusId           = (short)StudentStatusEnum.Active,
            CreatedOn          = DateTimeOffset.UtcNow,
            CreatedByEmail     = model.CreatedByEmail,
            CreatedByName      = model.CreatedByName
        };

    public static Student ApplyUpdate(this Student student, StudentModel model)
    {
        student.FirstName          = model.FirstName.Trim();
        student.LastName           = model.LastName.Trim();
        student.Email              = model.Email?.Trim().ToLowerInvariant();
        student.PhoneNumber        = model.PhoneNumber.Trim();
        student.DateOfBirth        = model.DateOfBirth;
        student.Gender             = Enum.TryParse<GenderEnum>(model.Gender, ignoreCase: true, out var gender) ? gender : student.Gender;
        student.StartDate          = model.StartDate;
        student.BeltLevel          = Enum.TryParse<BeltLevelEnum>(model.BeltLevel, ignoreCase: true, out var belt) ? belt : student.BeltLevel;
        student.SubscriptionPlanId = model.SubscriptionPlanId;
        // Only overwrite when a new image URL was supplied; null means "no change"
        if (model.ProfileImageUrl is not null)
            student.ProfileImageUrl = model.ProfileImageUrl.Trim();
        student.EmergencyContact   = model.EmergencyContact?.Trim();
        student.ModifiedOn         = DateTimeOffset.UtcNow;
        student.ModifiedByEmail    = model.ModifiedByEmail;
        student.ModifiedByName     = model.ModifiedByName;
        // Price and Currency are intentionally NOT updated here — they are frozen at registration
        return student;
    }
}
