using Identity.Application.Commands.Users;
using Identity.Application.Models.User;
using Identity.Application.Queries.Users;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shared.Domain.Enums;
using Shared.Domain.Pagination;
using Shared.Domain.Repositories;

namespace Identity.Application.Tests.Users;

internal static class UserTestData
{
    public static User Make(Guid? id = null, string username = "jdoe", string email = "jdoe@acme.test",
        short status = 1, params UserRoleEnum[] roles)
    {
        var user = new User
        {
            Id = id ?? Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = "hash",
            CreatedOn = DateTimeOffset.UtcNow,
            CreatedByEmail = "a@a.test",
            CreatedByName = "A",
            StatusId = status
        };
        foreach (var r in roles)
            user.UserRoles.Add(new UserRole { RoleId = r, UserId = user.Id });
        return user;
    }
}

public class DeleteUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private DeleteUserCommandHandler CreateSut() => new(_users, _uow);

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsUserNotFound()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateSut().Handle(new DeleteUserCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        Assert.Equal(UserErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenDeletingOtherUserAndNotAdmin_ReturnsForbidden()
    {
        var target = Guid.NewGuid();
        var requester = Guid.NewGuid();
        _users.GetByIdAsync(target, Arg.Any<CancellationToken>()).Returns(UserTestData.Make(target));
        _users.GetByIdAsync(requester, Arg.Any<CancellationToken>()).Returns(UserTestData.Make(requester, roles: UserRoleEnum.Coach));

        var result = await CreateSut().Handle(new DeleteUserCommand(target, requester), default);

        Assert.Equal(UserErrors.Forbidden, result.Error);
        _users.DidNotReceive().Update(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WhenAdminDeletesOtherUser_SoftDeletesAndSaves()
    {
        var target = Guid.NewGuid();
        var requester = Guid.NewGuid();
        var targetUser = UserTestData.Make(target);
        _users.GetByIdAsync(target, Arg.Any<CancellationToken>()).Returns(targetUser);
        _users.GetByIdAsync(requester, Arg.Any<CancellationToken>()).Returns(UserTestData.Make(requester, roles: UserRoleEnum.SuberAdmin));

        var result = await CreateSut().Handle(new DeleteUserCommand(target, requester), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserMessages.UserDeletedSuccessfully, result.Value);
        Assert.Equal((short)EntityStatusEnum.Deleted, targetUser.StatusId);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDeletingSelf_Succeeds()
    {
        var id = Guid.NewGuid();
        _users.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(UserTestData.Make(id));

        var result = await CreateSut().Handle(new DeleteUserCommand(id, id), default);

        Assert.True(result.IsSuccess);
    }
}

public class UpdateAccountCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly NullLogger<UpdateAccountCommandHandler> _log = NullLogger<UpdateAccountCommandHandler>.Instance;

    private UpdateAccountCommandHandler CreateSut() => new(_users, _uow, _log);

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsUserNotFound()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateSut().Handle(new UpdateAccountCommand(Guid.NewGuid(), new UpdateAccountModel()), default);

        Assert.Equal(UserErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNewEmailTaken_ReturnsEmailAlreadyExists()
    {
        var id = Guid.NewGuid();
        _users.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(UserTestData.Make(id, email: "old@acme.test"));
        _users.ExistsByEmailAsync("new@acme.test", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(
            new UpdateAccountCommand(id, new UpdateAccountModel { Email = "new@acme.test" }), default);

        Assert.Equal(UserErrors.EmailAlreadyExists, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesRolesEmailAndSaves()
    {
        var id = Guid.NewGuid();
        var user = UserTestData.Make(id, email: "old@acme.test", roles: UserRoleEnum.Coach);
        _users.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(user);
        _users.ExistsByEmailAsync("new@acme.test", Arg.Any<CancellationToken>()).Returns(false);

        var model = new UpdateAccountModel
        {
            Email = "new@acme.test",
            Active = false,
            PhoneNumber = "+100",
            Roles = new List<UserRoleEnum> { UserRoleEnum.Admin }
        };

        var result = await CreateSut().Handle(new UpdateAccountCommand(id, model), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("new@acme.test", user.Email);
        Assert.Equal((short)EntityStatusEnum.Inactive, user.StatusId);
        Assert.Single(user.UserRoles);
        Assert.Equal(UserRoleEnum.Admin, user.UserRoles.First().RoleId);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class UpdateProfileCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private UpdateProfileCommandHandler CreateSut() => new(_users, _uow);

    [Fact]
    public async Task Handle_WhenUsernameMissing_ReturnsUsernameRequired()
    {
        var result = await CreateSut().Handle(
            new UpdateProfileCommand(new UpdateUserModel { UserId = Guid.NewGuid(), Username = " " }), default);

        Assert.Equal(UserErrors.UsernameRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsUserNotFound()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateSut().Handle(
            new UpdateProfileCommand(new UpdateUserModel { UserId = Guid.NewGuid(), Username = "new" }), default);

        Assert.Equal(UserErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNewUsernameTaken_ReturnsUsernameAlreadyExists()
    {
        var id = Guid.NewGuid();
        _users.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(UserTestData.Make(id, username: "old"));
        _users.ExistsByUsernameAsync("taken", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(
            new UpdateProfileCommand(new UpdateUserModel { UserId = id, Username = "taken" }), default);

        Assert.Equal(UserErrors.UsernameAlreadyExists, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesAndSaves()
    {
        var id = Guid.NewGuid();
        var user = UserTestData.Make(id, username: "old");
        _users.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(user);
        _users.ExistsByUsernameAsync("newname", Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().Handle(
            new UpdateProfileCommand(new UpdateUserModel { UserId = id, Username = "newname", PhoneNumber = "+1" }), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("newname", user.Username);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class UserQueryHandlersTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();

    [Fact]
    public async Task GetCurrentUser_WhenNotFound_ReturnsUserNotFound()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await new GetCurrentUserQueryHandler(_users)
            .Handle(new GetCurrentUserQuery(Guid.NewGuid()), default);

        Assert.Equal(UserErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task GetCurrentUser_WhenFound_ReturnsProfile()
    {
        var id = Guid.NewGuid();
        _users.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(UserTestData.Make(id, username: "jdoe"));

        var result = await new GetCurrentUserQueryHandler(_users)
            .Handle(new GetCurrentUserQuery(id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("jdoe", result.Value.Username);
    }

    [Fact]
    public async Task GetUserById_WhenNotFound_ReturnsUserNotFound()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await new GetUserByIdQueryHandler(_users)
            .Handle(new GetUserByIdQuery(Guid.NewGuid()), default);

        Assert.Equal(UserErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task GetUserById_WhenFound_ReturnsProfile()
    {
        var id = Guid.NewGuid();
        _users.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(UserTestData.Make(id, username: "jdoe"));

        var result = await new GetUserByIdQueryHandler(_users)
            .Handle(new GetUserByIdQuery(id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
    }

    [Fact]
    public async Task GetAllUsers_MapsPagedResult()
    {
        var paged = PagedResult<User>.Create(new List<User> { UserTestData.Make() }, 1, 1, 20);
        _users.GetPagedAsync(Arg.Any<PagedRequest>(), Arg.Any<CancellationToken>()).Returns(paged);

        var result = await new GetAllUsersQueryHandler(_users)
            .Handle(new GetAllUsersQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
    }
}
