using Dojo.Application.Dtos.Classes;
using Dojo.Application.Dtos.Students;
using Dojo.Application.Mappings.Students;
using Dojo.Application.Models.Class;
using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Shared.Domain.Enums;

namespace Dojo.Application.Mappings.Classes;

public static class ClassMappings
{
    public static List<ClassDto> ToListDtos(this IEnumerable<Class> classes)
        => classes.Select(c => c.ToDto()).ToList();

    public static ClassDto ToDto(this Class trainingClass, List<StudentDto>? students = null)
        => new()
        {
            Id           = trainingClass.Id,
            TenantId     = trainingClass.TenantId,
            BranchId     = trainingClass.BranchId,
            Name         = trainingClass.Name,
            StartTime    = trainingClass.StartTime,
            EndTime      = trainingClass.EndTime,
            Weekdays     = trainingClass.Weekdays.Select(d => (int)d).ToList(),
            StudentCount = students?.Count
                ?? trainingClass.Students?.Count(s => s.StatusId == (short)StudentStatusEnum.Active)
                ?? 0,
            Status       = ((EntityStatusEnum)trainingClass.StatusId).ToString(),
            CreatedOn    = trainingClass.CreatedOn,
            ModifiedOn   = trainingClass.ModifiedOn,
            Students     = students
        };

    public static Class ToEntity(this ClassModel model, Guid branchId, Guid tenantId)
        => new()
        {
            BranchId       = branchId,
            TenantId       = tenantId,
            Name           = model.Name.Trim(),
            StartTime      = model.StartTime,
            EndTime        = model.EndTime,
            Weekdays       = model.Weekdays.Distinct().ToList(),
            StatusId       = (short)EntityStatusEnum.Active,
            CreatedOn      = DateTimeOffset.UtcNow,
            CreatedByEmail = model.CreatedByEmail,
            CreatedByName  = model.CreatedByName
        };

    public static Class ApplyUpdate(this Class trainingClass, ClassModel model)
    {
        trainingClass.Name            = model.Name.Trim();
        trainingClass.StartTime       = model.StartTime;
        trainingClass.EndTime         = model.EndTime;
        trainingClass.Weekdays        = model.Weekdays.Distinct().ToList();
        trainingClass.ModifiedOn      = DateTimeOffset.UtcNow;
        trainingClass.ModifiedByEmail = model.ModifiedByEmail;
        trainingClass.ModifiedByName  = model.ModifiedByName;
        return trainingClass;
    }

    public static List<StudentClassSummaryDto> ToClassSummaryDtos(this IEnumerable<Student> students)
        => students.Select(s => s.ToClassSummaryDto()).ToList();

    public static StudentClassSummaryDto ToClassSummaryDto(this Student student)
        => new()
        {
            StudentId = student.Id,
            Name      = student.FullName,
            ClassName = student.Class?.Name,
            BeltLevel = student.BeltLevel.ToString(),
            Age       = CalculateAge(student.DateOfBirth)
        };

    private static int CalculateAge(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age))
            age--;
        return age;
    }
}
