using Dojo.Application.Services;
using Dojo.Application.Tests.Students;
using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shared.Application.Contracts;
using Shared.Application.Models;
using Shared.Domain.Repositories;

namespace Dojo.Application.Tests.Services;

public class StudentExpiryProcessorTests
{
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly IStudentActivityLogRepository _activityLogs = Substitute.For<IStudentActivityLogRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly INotificationTargetsService _notificationTargets = Substitute.For<INotificationTargetsService>();
    private readonly IWhatsAppService _whatsApp = Substitute.For<IWhatsAppService>();

    private StudentExpiryProcessor CreateSut() =>
        new(_students, _activityLogs, _uow, _notificationTargets, _whatsApp, NullLogger<StudentExpiryProcessor>.Instance);

    [Fact]
    public async Task ProcessExpiredStudentsAsync_WhenNoneExpired_ReturnsZeroAndSendsNothing()
    {
        _students.GetExpiredActiveStudentsAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns([]);

        var processed = await CreateSut().ProcessExpiredStudentsAsync(default);

        Assert.Equal(0, processed);
        await _whatsApp.DidNotReceiveWithAnyArgs().SendMessageAsync(default!, default!, default);
    }

    [Fact]
    public async Task ProcessExpiredStudentsAsync_DeactivatesAndNotifiesStudentAndAdmins()
    {
        var student = StudentTestData.Make();
        student.PhoneNumber = "0700000001";
        _students.GetExpiredActiveStudentsAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns([student]);

        var admin = new NotificationTarget { Name = "Admin One", PhoneNumber = "0700000002", Role = "Admin" };
        _notificationTargets.GetAdminsAndSuperAdminsAsync(student.TenantId, student.BranchId, Arg.Any<CancellationToken>())
            .Returns([admin]);

        var processed = await CreateSut().ProcessExpiredStudentsAsync(default);

        Assert.Equal(1, processed);
        Assert.Equal((short)StudentStatusEnum.Inactive, student.StatusId);
        _students.Received(1).Update(student);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _whatsApp.Received(1).SendMessageAsync("0700000001", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _whatsApp.Received(1).SendMessageAsync("0700000002", Arg.Any<string>(), Arg.Any<CancellationToken>());
        _activityLogs.Received(1).Add(Arg.Is<StudentActivityLog>(l =>
            l.StudentId == student.Id && l.ActivityType == StudentActivityType.ExpiredAutomatically));
    }

    [Fact]
    public async Task ProcessExpiredStudentsAsync_WhenStudentHasNoPhone_StillNotifiesAdmins()
    {
        var student = StudentTestData.Make();
        student.PhoneNumber = "";
        _students.GetExpiredActiveStudentsAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns([student]);

        var admin = new NotificationTarget { Name = "Admin", PhoneNumber = "0700000002", Role = "Admin" };
        _notificationTargets.GetAdminsAndSuperAdminsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([admin]);

        await CreateSut().ProcessExpiredStudentsAsync(default);

        await _whatsApp.Received(1).SendMessageAsync("0700000002", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _whatsApp.Received(1).SendMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessExpiredStudentsAsync_WhenTargetHasNoPhone_SkipsThatTarget()
    {
        var student = StudentTestData.Make();
        student.PhoneNumber = "0700000001";
        _students.GetExpiredActiveStudentsAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns([student]);

        var adminNoPhone = new NotificationTarget { Name = "No Phone", PhoneNumber = null, Role = "Admin" };
        _notificationTargets.GetAdminsAndSuperAdminsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([adminNoPhone]);

        await CreateSut().ProcessExpiredStudentsAsync(default);

        // Only the student's own message goes out — the phoneless admin is skipped, not errored.
        await _whatsApp.Received(1).SendMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessExpiredStudentsAsync_WhenOneStudentFails_StillProcessesTheRest()
    {
        var failing = StudentTestData.Make();
        var succeeding = StudentTestData.Make();
        _students.GetExpiredActiveStudentsAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([failing, succeeding]);
        _notificationTargets.GetAdminsAndSuperAdminsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var callCount = 0;
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            callCount++;
            if (callCount == 1)
                throw new InvalidOperationException("simulated DB failure");
            return 1;
        });

        var processed = await CreateSut().ProcessExpiredStudentsAsync(default);

        Assert.Equal(1, processed); // only the second student counted as successfully processed
        Assert.Equal((short)StudentStatusEnum.Inactive, succeeding.StatusId);
    }
}
