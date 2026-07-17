using Dojo.Application.Dtos.Students;
using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Shared.Domain.Enums;

namespace Dojo.Application.Mappings.Students;

public static class StudentActivityLogMappings
{
    public static List<StudentActivityLogDto> ToListDtos(this IEnumerable<StudentActivityLog> logs)
        => logs.Select(l => l.ToDto()).ToList();

    public static StudentActivityLogDto ToDto(this StudentActivityLog log)
        => new()
        {
            Id             = log.Id,
            StudentId      = log.StudentId,
            ActivityType   = log.ActivityType.ToString(),
            Description    = log.Description,
            CreatedOn      = log.CreatedOn,
            CreatedByEmail = log.CreatedByEmail,
            CreatedByName  = log.CreatedByName
        };

    public static StudentActivityLog NewLog(
        Guid tenantId,
        Guid branchId,
        Guid studentId,
        StudentActivityType activityType,
        string description,
        string performedByEmail,
        string performedByName)
        => new()
        {
            TenantId       = tenantId,
            BranchId       = branchId,
            StudentId      = studentId,
            ActivityType   = activityType,
            Description    = description,
            StatusId       = (short)EntityStatusEnum.Active,
            CreatedOn      = DateTimeOffset.UtcNow,
            CreatedByEmail = performedByEmail,
            CreatedByName  = performedByName
        };
}
