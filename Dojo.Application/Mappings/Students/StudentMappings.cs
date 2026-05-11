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
            Id             = student.Id,
            TenantId       = student.TenantId,
            BranchId       = student.BranchId,
            FirstName      = student.FirstName,
            LastName       = student.LastName,
            FullName       = student.FullName,
            Email          = student.Email,
            PhoneNumber    = student.PhoneNumber,
            DateOfBirth    = student.DateOfBirth,
            Gender         = student.Gender.ToString(),
            BeltLevel      = student.BeltLevel.ToString(),
            EnrollmentDate = student.EnrollmentDate,
            Enabled        = student.Enabled
        };

    public static Student ToEntity(this StudentModel model)
        => new()
        {
            BranchId       = model.BranchId,
            FirstName      = model.FirstName.Trim(),
            LastName       = model.LastName.Trim(),
            Email          = model.Email.Trim().ToLowerInvariant(),
            PhoneNumber    = model.PhoneNumber?.Trim(),
            DateOfBirth    = model.DateOfBirth,
            Gender         = Enum.TryParse<GenderEnum>(model.Gender, ignoreCase: true, out var gender) ? gender : GenderEnum.Other,
            BeltLevel      = Enum.TryParse<BeltLevelEnum>(model.BeltLevel, ignoreCase: true, out var belt) ? belt : BeltLevelEnum.White,
            EnrollmentDate = model.EnrollmentDate,
            Enabled        = model.Enabled,
            StatusId       = (short)EntityStatusEnum.Active,
            CreatedOn      = DateTimeOffset.UtcNow,
            CreatedByEmail = model.CreatedByEmail,
            CreatedByName  = model.CreatedByName
        };

    public static Student ApplyUpdate(this Student student, StudentModel model)
    {
        student.BranchId       = model.BranchId;
        student.FirstName      = model.FirstName.Trim();
        student.LastName       = model.LastName.Trim();
        student.Email          = model.Email.Trim().ToLowerInvariant();
        student.PhoneNumber    = model.PhoneNumber?.Trim();
        student.DateOfBirth    = model.DateOfBirth;
        student.Gender         = Enum.TryParse<GenderEnum>(model.Gender, ignoreCase: true, out var gender) ? gender : student.Gender;
        student.BeltLevel      = Enum.TryParse<BeltLevelEnum>(model.BeltLevel, ignoreCase: true, out var belt) ? belt : student.BeltLevel;
        student.EnrollmentDate = model.EnrollmentDate;
        student.Enabled        = model.Enabled;
        student.ModifiedOn     = DateTimeOffset.UtcNow;
        student.ModifiedByEmail = model.ModifiedByEmail;
        student.ModifiedByName  = model.ModifiedByName;
        return student;
    }
}
