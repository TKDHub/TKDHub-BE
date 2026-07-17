using Dojo.Application.Commands.Classes;
using Dojo.Application.Models.Class;
using Dojo.Application.Queries.Classes;
using Dojo.Application.Tests.Students;
using Dojo.Domain.Constants;
using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shared.Application.Contracts;
using Shared.Application.Models;
using Shared.Domain.Enums;
using Shared.Domain.Pagination;
using Shared.Domain.Repositories;

namespace Dojo.Application.Tests.Classes;

internal static class ClassTestData
{
    public static Class Make(Guid? id = null, Guid? branchId = null, string name = "Kids Beginners", short status = 1) => new()
    {
        Id = id ?? Guid.NewGuid(),
        BranchId = branchId ?? Guid.NewGuid(),
        Name = name,
        StartTime = new TimeOnly(16, 0),
        EndTime = new TimeOnly(17, 0),
        Weekdays = [DayOfWeek.Monday, DayOfWeek.Wednesday],
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "a@a.test",
        CreatedByName = "A",
        StatusId = status
    };

    public static ClassModel Model() => new()
    {
        Name = "Kids Beginners",
        StartTime = new TimeOnly(16, 0),
        EndTime = new TimeOnly(17, 0),
        Weekdays = [DayOfWeek.Monday, DayOfWeek.Wednesday]
    };
}

public class CreateClassCommandHandlerTests
{
    private readonly IClassRepository _classes = Substitute.For<IClassRepository>();
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly IBranchService _branchService = Substitute.For<IBranchService>();
    private readonly IStudentActivityLogRepository _activityLogs = Substitute.For<IStudentActivityLogRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateClassCommandHandler CreateSut() => new(_classes, _students, _branchService, _activityLogs, _uow, NullLogger<CreateClassCommandHandler>.Instance);

    [Fact]
    public async Task Handle_WhenBranchIdEmpty_ReturnsBranchRequired()
    {
        var result = await CreateSut().Handle(new CreateClassCommand(ClassTestData.Model(), Guid.Empty, Guid.NewGuid()), default);
        Assert.Equal(ClassErrors.BranchRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenEndTimeNotAfterStartTime_ReturnsInvalidTimeRange()
    {
        var model = ClassTestData.Model() with { EndTime = new TimeOnly(15, 0) };
        var result = await CreateSut().Handle(new CreateClassCommand(model, Guid.NewGuid(), Guid.NewGuid()), default);
        Assert.Equal(ClassErrors.InvalidTimeRange, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNoWeekdays_ReturnsWeekdaysRequired()
    {
        var model = ClassTestData.Model() with { Weekdays = [] };
        var result = await CreateSut().Handle(new CreateClassCommand(model, Guid.NewGuid(), Guid.NewGuid()), default);
        Assert.Equal(ClassErrors.WeekdaysRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenBranchNotFound_ReturnsBranchNotFound()
    {
        var branchId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>()).Returns((BranchInfo?)null);

        var result = await CreateSut().Handle(new CreateClassCommand(ClassTestData.Model(), branchId, Guid.NewGuid()), default);

        Assert.Equal(ClassErrors.BranchNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenTenantMismatch_ReturnsTenantBranchMismatch()
    {
        var branchId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = Guid.NewGuid(), Currency = "JOD", Enabled = true });

        var result = await CreateSut().Handle(new CreateClassCommand(ClassTestData.Model(), branchId, Guid.NewGuid()), default);

        Assert.Equal(ClassErrors.TenantBranchMismatch, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ReturnsNameAlreadyExists()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = tenantId, Currency = "JOD", Enabled = true });
        _classes.ExistsByNameAsync("Kids Beginners", branchId, null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(new CreateClassCommand(ClassTestData.Model(), branchId, tenantId), default);

        Assert.Equal(ClassErrors.NameAlreadyExists, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesClassAndSaves()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = tenantId, Currency = "JOD", Enabled = true });
        _classes.ExistsByNameAsync(Arg.Any<string>(), branchId, null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().Handle(new CreateClassCommand(ClassTestData.Model(), branchId, tenantId), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Kids Beginners", result.Value.Name);
        Assert.Equal(["Monday", "Wednesday"], result.Value.Weekdays);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStudentIdsProvided_MovesThemIntoTheNewClass()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var student = StudentTestData.Make();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = tenantId, Currency = "JOD", Enabled = true });
        _classes.ExistsByNameAsync(Arg.Any<string>(), branchId, null, Arg.Any<CancellationToken>()).Returns(false);
        _students.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([student]);

        var model = ClassTestData.Model() with { StudentIds = [student.Id] };
        var result = await CreateSut().Handle(new CreateClassCommand(model, branchId, tenantId), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(result.Value.Id, student.ClassId);
        _students.Received(1).Update(student);
        _activityLogs.Received(1).Add(Arg.Is<StudentActivityLog>(l =>
            l.StudentId == student.Id && l.ActivityType == StudentActivityType.AddedToClass));
    }

    [Fact]
    public async Task Handle_WhenStudentIdNotFound_ReturnsStudentNotFound()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = tenantId, Currency = "JOD", Enabled = true });
        _classes.ExistsByNameAsync(Arg.Any<string>(), branchId, null, Arg.Any<CancellationToken>()).Returns(false);
        _students.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);

        var model = ClassTestData.Model() with { StudentIds = [Guid.NewGuid()] };
        var result = await CreateSut().Handle(new CreateClassCommand(model, branchId, tenantId), default);

        Assert.Equal(ClassErrors.StudentNotFound, result.Error);
    }
}

public class UpdateClassCommandHandlerTests
{
    private readonly IClassRepository _classes = Substitute.For<IClassRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private UpdateClassCommandHandler CreateSut() => new(_classes, _uow, NullLogger<UpdateClassCommandHandler>.Instance);

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var model = ClassTestData.Model() with { ClassId = Guid.NewGuid() };
        _classes.GetByIdAsync(model.ClassId!.Value, Arg.Any<CancellationToken>()).Returns((Class?)null);

        var result = await CreateSut().Handle(new UpdateClassCommand(model), default);

        Assert.Equal(ClassErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenRenamedToExistingName_ReturnsNameAlreadyExists()
    {
        var trainingClass = ClassTestData.Make(name: "Original");
        _classes.GetByIdAsync(trainingClass.Id, Arg.Any<CancellationToken>()).Returns(trainingClass);
        _classes.ExistsByNameAsync("Renamed", trainingClass.BranchId, trainingClass.Id, Arg.Any<CancellationToken>()).Returns(true);

        var model = ClassTestData.Model() with { ClassId = trainingClass.Id, Name = "Renamed" };
        var result = await CreateSut().Handle(new UpdateClassCommand(model), default);

        Assert.Equal(ClassErrors.NameAlreadyExists, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesAndSaves()
    {
        var trainingClass = ClassTestData.Make();
        _classes.GetByIdAsync(trainingClass.Id, Arg.Any<CancellationToken>()).Returns(trainingClass);

        var model = ClassTestData.Model() with { ClassId = trainingClass.Id, Name = "Kids Beginners", StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0) };
        var result = await CreateSut().Handle(new UpdateClassCommand(model), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(new TimeOnly(18, 0), trainingClass.StartTime);
        _classes.Received(1).Update(trainingClass);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class DeleteClassCommandHandlerTests
{
    private readonly IClassRepository _classes = Substitute.For<IClassRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private DeleteClassCommandHandler CreateSut() => new(_classes, _uow, NullLogger<DeleteClassCommandHandler>.Instance);

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _classes.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Class?)null);

        var result = await CreateSut().Handle(new DeleteClassCommand(Guid.NewGuid()), default);

        Assert.Equal(ClassErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenHasActiveStudents_ReturnsHasActiveStudents()
    {
        var trainingClass = ClassTestData.Make();
        _classes.GetByIdAsync(trainingClass.Id, Arg.Any<CancellationToken>()).Returns(trainingClass);
        _classes.HasActiveStudentsAsync(trainingClass.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(new DeleteClassCommand(trainingClass.Id), default);

        Assert.Equal(ClassErrors.HasActiveStudents, result.Error);
        _classes.DidNotReceive().Update(Arg.Any<Class>());
    }

    [Fact]
    public async Task Handle_WhenNoActiveStudents_SoftDeletesAndSaves()
    {
        var trainingClass = ClassTestData.Make();
        _classes.GetByIdAsync(trainingClass.Id, Arg.Any<CancellationToken>()).Returns(trainingClass);
        _classes.HasActiveStudentsAsync(trainingClass.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().Handle(new DeleteClassCommand(trainingClass.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal((short)EntityStatusEnum.Deleted, trainingClass.StatusId);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class AddStudentsToClassCommandHandlerTests
{
    private readonly IClassRepository _classes = Substitute.For<IClassRepository>();
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly IStudentActivityLogRepository _activityLogs = Substitute.For<IStudentActivityLogRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private AddStudentsToClassCommandHandler CreateSut() => new(_classes, _students, _activityLogs, _uow, NullLogger<AddStudentsToClassCommandHandler>.Instance);

    [Fact]
    public async Task Handle_WhenNoStudentIds_ReturnsNoStudentsProvided()
    {
        var result = await CreateSut().Handle(
            new AddStudentsToClassCommand(new AddStudentsToClassModel { ClassId = Guid.NewGuid(), StudentIds = [] }), default);

        Assert.Equal(ClassErrors.NoStudentsProvided, result.Error);
    }

    [Fact]
    public async Task Handle_WhenClassNotFound_ReturnsNotFound()
    {
        _classes.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Class?)null);

        var result = await CreateSut().Handle(
            new AddStudentsToClassCommand(new AddStudentsToClassModel { ClassId = Guid.NewGuid(), StudentIds = [Guid.NewGuid()] }), default);

        Assert.Equal(ClassErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenStudentAlreadyInAnotherClass_MovesThemToTheNewOne()
    {
        var trainingClass = ClassTestData.Make();
        var otherClassId = Guid.NewGuid();
        var student = StudentTestData.Make();
        student.ClassId = otherClassId;

        _classes.GetByIdAsync(trainingClass.Id, Arg.Any<CancellationToken>()).Returns(trainingClass);
        _students.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([student]);
        _classes.GetByIdWithStudentsAsync(trainingClass.Id, Arg.Any<CancellationToken>()).Returns(trainingClass);

        var result = await CreateSut().Handle(
            new AddStudentsToClassCommand(new AddStudentsToClassModel { ClassId = trainingClass.Id, StudentIds = [student.Id] }), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(trainingClass.Id, student.ClassId);
        _students.Received(1).Update(student);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _activityLogs.Received(1).Add(Arg.Is<StudentActivityLog>(l =>
            l.StudentId == student.Id && l.ActivityType == StudentActivityType.AddedToClass));
    }

    [Fact]
    public async Task Handle_WhenAStudentIdDoesNotExist_ReturnsStudentNotFound()
    {
        var trainingClass = ClassTestData.Make();
        _classes.GetByIdAsync(trainingClass.Id, Arg.Any<CancellationToken>()).Returns(trainingClass);
        _students.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateSut().Handle(
            new AddStudentsToClassCommand(new AddStudentsToClassModel { ClassId = trainingClass.Id, StudentIds = [Guid.NewGuid()] }), default);

        Assert.Equal(ClassErrors.StudentNotFound, result.Error);
    }
}

public class RemoveStudentsFromClassCommandHandlerTests
{
    private readonly IClassRepository _classes = Substitute.For<IClassRepository>();
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly IStudentActivityLogRepository _activityLogs = Substitute.For<IStudentActivityLogRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private RemoveStudentsFromClassCommandHandler CreateSut() => new(_classes, _students, _activityLogs, _uow, NullLogger<RemoveStudentsFromClassCommandHandler>.Instance);

    [Fact]
    public async Task Handle_WhenStudentBelongsToClass_DetachesThem()
    {
        var trainingClass = ClassTestData.Make();
        var student = StudentTestData.Make();
        student.ClassId = trainingClass.Id;

        _classes.GetByIdAsync(trainingClass.Id, Arg.Any<CancellationToken>()).Returns(trainingClass);
        _students.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([student]);
        _classes.GetByIdWithStudentsAsync(trainingClass.Id, Arg.Any<CancellationToken>()).Returns(trainingClass);

        var result = await CreateSut().Handle(
            new RemoveStudentsFromClassCommand(new RemoveStudentsFromClassModel { ClassId = trainingClass.Id, StudentIds = [student.Id] }), default);

        Assert.True(result.IsSuccess);
        Assert.Null(student.ClassId);
        _students.Received(1).Update(student);
        _activityLogs.Received(1).Add(Arg.Is<StudentActivityLog>(l =>
            l.StudentId == student.Id && l.ActivityType == StudentActivityType.RemovedFromClass));
    }

    [Fact]
    public async Task Handle_WhenStudentBelongsToDifferentClass_IsIgnored()
    {
        var trainingClass = ClassTestData.Make();
        var student = StudentTestData.Make();
        student.ClassId = Guid.NewGuid(); // some other class

        _classes.GetByIdAsync(trainingClass.Id, Arg.Any<CancellationToken>()).Returns(trainingClass);
        _students.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([student]);
        _classes.GetByIdWithStudentsAsync(trainingClass.Id, Arg.Any<CancellationToken>()).Returns(trainingClass);

        var result = await CreateSut().Handle(
            new RemoveStudentsFromClassCommand(new RemoveStudentsFromClassModel { ClassId = trainingClass.Id, StudentIds = [student.Id] }), default);

        Assert.True(result.IsSuccess);
        _students.DidNotReceive().Update(Arg.Any<Student>());
    }
}

public class MoveStudentsToClassCommandHandlerTests
{
    private readonly IClassRepository _classes = Substitute.For<IClassRepository>();
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly IStudentActivityLogRepository _activityLogs = Substitute.For<IStudentActivityLogRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private MoveStudentsToClassCommandHandler CreateSut() => new(_classes, _students, _activityLogs, _uow, NullLogger<MoveStudentsToClassCommandHandler>.Instance);

    [Fact]
    public async Task Handle_WhenFromAndToAreSame_ReturnsSameClass()
    {
        var classId = Guid.NewGuid();
        var result = await CreateSut().Handle(
            new MoveStudentsToClassCommand(new MoveStudentsToClassModel { FromClassId = classId, ToClassId = classId, StudentIds = [Guid.NewGuid()] }), default);

        Assert.Equal(ClassErrors.SameClass, result.Error);
    }

    [Fact]
    public async Task Handle_WhenTargetClassNotFound_ReturnsTargetClassNotFound()
    {
        var source = ClassTestData.Make();
        _classes.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);
        _classes.GetByIdAsync(Arg.Is<Guid>(g => g != source.Id), Arg.Any<CancellationToken>()).Returns((Class?)null);

        var result = await CreateSut().Handle(
            new MoveStudentsToClassCommand(new MoveStudentsToClassModel { FromClassId = source.Id, ToClassId = Guid.NewGuid(), StudentIds = [Guid.NewGuid()] }), default);

        Assert.Equal(ClassErrors.TargetClassNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_MovesOnlyStudentsBelongingToSourceClass()
    {
        var source = ClassTestData.Make();
        var target = ClassTestData.Make();
        var studentInSource = StudentTestData.Make();
        studentInSource.ClassId = source.Id;
        var studentElsewhere = StudentTestData.Make();
        studentElsewhere.ClassId = Guid.NewGuid();

        _classes.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);
        _classes.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        _students.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([studentInSource, studentElsewhere]);
        _classes.GetByIdWithStudentsAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);

        var result = await CreateSut().Handle(
            new MoveStudentsToClassCommand(new MoveStudentsToClassModel
            {
                FromClassId = source.Id,
                ToClassId = target.Id,
                StudentIds = [studentInSource.Id, studentElsewhere.Id]
            }), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(target.Id, studentInSource.ClassId);
        Assert.NotEqual(target.Id, studentElsewhere.ClassId); // untouched — wasn't in the source class
        _students.Received(1).Update(studentInSource);
        _students.DidNotReceive().Update(studentElsewhere);
        _activityLogs.Received(1).Add(Arg.Is<StudentActivityLog>(l =>
            l.StudentId == studentInSource.Id && l.ActivityType == StudentActivityType.MovedClass));
    }
}

public class GetClassByIdQueryHandlerTests
{
    private readonly IClassRepository _classes = Substitute.For<IClassRepository>();

    private GetClassByIdQueryHandler CreateSut() => new(_classes, NullLogger<GetClassByIdQueryHandler>.Instance);

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _classes.GetByIdWithStudentsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Class?)null);

        var result = await CreateSut().Handle(new GetClassByIdQuery(Guid.NewGuid()), default);

        Assert.Equal(ClassErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenFound_ReturnsDtoWithStudents()
    {
        var trainingClass = ClassTestData.Make();
        var student = StudentTestData.Make();
        trainingClass.Students.Add(student);
        _classes.GetByIdWithStudentsAsync(trainingClass.Id, Arg.Any<CancellationToken>()).Returns(trainingClass);

        var result = await CreateSut().Handle(new GetClassByIdQuery(trainingClass.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Students!);
    }
}

public class GetAllClassesQueryHandlerTests
{
    private readonly IClassRepository _classes = Substitute.For<IClassRepository>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IBranchContext _branchContext = Substitute.For<IBranchContext>();

    private GetAllClassesQueryHandler CreateSut() => new(_classes, _userContext, _branchContext, NullLogger<GetAllClassesQueryHandler>.Instance);

    [Fact]
    public async Task Handle_WhenBranchAdmin_ScopesToBranch()
    {
        var branchId = Guid.NewGuid();
        _userContext.IsSuperAdmin.Returns(false);
        _branchContext.BranchId.Returns(branchId);
        _classes.GetPagedAsync(Arg.Any<PagedRequest>(), branchId, Arg.Any<CancellationToken>())
            .Returns(PagedResult<Class>.Empty(1, 20));

        var result = await CreateSut().Handle(new GetAllClassesQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        await _classes.Received(1).GetPagedAsync(Arg.Any<PagedRequest>(), branchId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSuperAdmin_QueriesAcrossAllBranches()
    {
        _userContext.IsSuperAdmin.Returns(true);
        _classes.GetPagedAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(PagedResult<Class>.Empty(1, 20));

        var result = await CreateSut().Handle(new GetAllClassesQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        await _classes.Received(1).GetPagedAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>());
    }
}

public class GetStudentsWithClassesQueryHandlerTests
{
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IBranchContext _branchContext = Substitute.For<IBranchContext>();

    private GetStudentsWithClassesQueryHandler CreateSut() => new(_students, _userContext, _branchContext, NullLogger<GetStudentsWithClassesQueryHandler>.Instance);

    [Fact]
    public async Task Handle_ReturnsNameClassNameBeltAndAge()
    {
        _userContext.IsSuperAdmin.Returns(true);
        var trainingClass = ClassTestData.Make(name: "Kids Beginners");
        var student = StudentTestData.Make(firstName: "Sara", lastName: "Ahmad");
        student.DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10));
        student.Class = trainingClass;

        _students.GetPagedWithClassAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(PagedResult<Student>.Create([student], 1, 1, 20));

        var result = await CreateSut().Handle(new GetStudentsWithClassesQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Value.Items);
        Assert.Equal("Sara Ahmad", row.Name);
        Assert.Equal("Kids Beginners", row.ClassName);
        Assert.Equal(10, row.Age);
    }

    [Fact]
    public async Task Handle_WhenStudentHasNoClass_ClassNameIsNull()
    {
        _userContext.IsSuperAdmin.Returns(true);
        var student = StudentTestData.Make();

        _students.GetPagedWithClassAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(PagedResult<Student>.Create([student], 1, 1, 20));

        var result = await CreateSut().Handle(new GetStudentsWithClassesQuery(new PagedRequest()), default);

        Assert.Null(result.Value.Items[0].ClassName);
    }
}
