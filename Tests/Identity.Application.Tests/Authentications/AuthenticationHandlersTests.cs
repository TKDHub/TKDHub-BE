using Identity.Application.Commands.Authentications;
using Identity.Application.Contracts;
using Identity.Application.Models.Auth;
using Identity.Application.Models.User;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;
using Shared.Domain.Repositories;

namespace Identity.Application.Tests.Authentications;

internal static class AuthTestData
{
    public static User MakeUser(Guid? id = null, string username = "jdoe", string email = "jdoe@acme.test",
        string passwordHash = "hashed", short status = 1, Guid? tenantId = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId ?? Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            CreatedOn = DateTimeOffset.UtcNow,
            CreatedByEmail = "a@a.test",
            CreatedByName = "A",
            StatusId = status
        };

    public static Tenant MakeTenant(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "Acme",
        Subdomain = "acme",
        ContactEmail = "owner@acme.test",
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "a@a.test",
        CreatedByName = "A",
        StatusId = 1
    };

    public static AuthenticationResponse MakeAuthResponse(Guid userId) =>
        new(userId, "jdoe", "jdoe@acme.test", "access-token", "refresh-token", DateTime.UtcNow.AddHours(1));
}

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IAuthenticationService _authService = Substitute.For<IAuthenticationService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private LoginCommandHandler CreateSut() =>
        new(_users, _hasher, _authService, _uow, _tenants, NullLogger<LoginCommandHandler>.Instance);

    private static AuthModel Model(string username = "jdoe", string password = "P@ss123") =>
        new() { Username = username, Password = password };

    [Fact]
    public async Task Handle_WhenUserNotFoundByUsernameOrEmail_ReturnsInvalidCredentials()
    {
        _users.GetByUsernameAsync("jdoe", Arg.Any<CancellationToken>()).Returns((User?)null);
        _users.GetByEmailAsync("jdoe", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateSut().Handle(new LoginCommand(Model()), default);

        Assert.Equal(UserErrors.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Handle_WhenAccountNotActive_ReturnsAccountNotActive()
    {
        var user = AuthTestData.MakeUser(status: (short)EntityStatusEnum.Inactive);
        _users.GetByUsernameAsync("jdoe", Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateSut().Handle(new LoginCommand(Model()), default);

        Assert.Equal(UserErrors.AccountNotActive, result.Error);
    }

    [Fact]
    public async Task Handle_WhenAccountLockedOut_ReturnsAccountLockedOut()
    {
        var user = AuthTestData.MakeUser();
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
        _users.GetByUsernameAsync("jdoe", Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateSut().Handle(new LoginCommand(Model()), default);

        Assert.Equal(UserErrors.AccountLockedOut, result.Error);
    }

    [Fact]
    public async Task Handle_WhenPasswordInvalid_RecordsFailedAttemptAndReturnsInvalidCredentials()
    {
        var user = AuthTestData.MakeUser();
        _users.GetByUsernameAsync("jdoe", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.VerifyPassword("wrong", user.PasswordHash).Returns(false);

        var result = await CreateSut().Handle(new LoginCommand(Model(password: "wrong")), default);

        Assert.Equal(UserErrors.InvalidCredentials, result.Error);
        Assert.Equal(1, user.FailedLoginAttempts);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ReturnsTenantNotFound()
    {
        var user = AuthTestData.MakeUser();
        _users.GetByUsernameAsync("jdoe", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.VerifyPassword("P@ss123", user.PasswordHash).Returns(true);
        _tenants.GetByIdAsync(user.TenantId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var result = await CreateSut().Handle(new LoginCommand(Model()), default);

        Assert.Equal(TenantErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_ReturnsAuthDtoAndPersistsRefreshToken()
    {
        var user = AuthTestData.MakeUser();
        var tenant = AuthTestData.MakeTenant(user.TenantId);
        var authResponse = AuthTestData.MakeAuthResponse(user.Id);

        _users.GetByUsernameAsync("jdoe", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.VerifyPassword("P@ss123", user.PasswordHash).Returns(true);
        _tenants.GetByIdAsync(user.TenantId, Arg.Any<CancellationToken>()).Returns(tenant);
        _authService.GenerateToken(user, tenant).Returns(authResponse);

        var result = await CreateSut().Handle(new LoginCommand(Model()), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("refresh-token", user.RefreshToken);
        Assert.Equal(0, user.FailedLoginAttempts);
        _users.Received(1).Update(user);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class RegisterCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IBranchRepository _branches = Substitute.For<IBranchRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private RegisterCommandHandler CreateSut() =>
        new(_users, _tenants, _branches, _hasher, _uow, NullLogger<RegisterCommandHandler>.Instance);

    private static RegisterUserModel Model(Guid? tenantId = null) => new()
    {
        TenantId = tenantId ?? Guid.NewGuid(),
        Username = "jdoe",
        Email = "jdoe@acme.test",
        Password = "P@ss123",
        ConfirmPassword = "P@ss123"
    };

    [Fact]
    public async Task Handle_WhenUsernameMissing_ReturnsUsernameRequired()
    {
        var result = await CreateSut().Handle(new RegisterCommand(Model() with { Username = " " }), default);
        Assert.Equal(UserErrors.UsernameRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenEmailMissing_ReturnsEmailRequired()
    {
        var result = await CreateSut().Handle(new RegisterCommand(Model() with { Email = "" }), default);
        Assert.Equal(UserErrors.EmailRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenEmailFormatInvalid_ReturnsInvalidEmailFormat()
    {
        var result = await CreateSut().Handle(new RegisterCommand(Model() with { Email = "not-an-email" }), default);
        Assert.Equal(UserErrors.InvalidEmailFormat, result.Error);
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ReturnsTenantNotFound()
    {
        var tenantId = Guid.NewGuid();
        _tenants.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var result = await CreateSut().Handle(new RegisterCommand(Model(tenantId)), default);

        Assert.Equal(TenantErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenUsernameTaken_ReturnsUsernameAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        _tenants.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(AuthTestData.MakeTenant(tenantId));
        _users.ExistsByUsernameAsync("jdoe", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(new RegisterCommand(Model(tenantId)), default);

        Assert.Equal(UserErrors.UsernameAlreadyExists, result.Error);
    }

    [Fact]
    public async Task Handle_WhenEmailTaken_ReturnsEmailAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        _tenants.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(AuthTestData.MakeTenant(tenantId));
        _users.ExistsByUsernameAsync("jdoe", Arg.Any<CancellationToken>()).Returns(false);
        _users.ExistsByEmailAsync("jdoe@acme.test", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(new RegisterCommand(Model(tenantId)), default);

        Assert.Equal(UserErrors.EmailAlreadyExists, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNoRolesProvided_DefaultsToStudentRole()
    {
        var tenantId = Guid.NewGuid();
        _tenants.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(AuthTestData.MakeTenant(tenantId));
        _users.ExistsByUsernameAsync("jdoe", Arg.Any<CancellationToken>()).Returns(false);
        _users.ExistsByEmailAsync("jdoe@acme.test", Arg.Any<CancellationToken>()).Returns(false);
        _hasher.HashPassword("P@ss123").Returns("hashed");

        User? added = null;
        _users.When(r => r.Add(Arg.Any<User>())).Do(c => added = c.Arg<User>());

        var result = await CreateSut().Handle(new RegisterCommand(Model(tenantId)), default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Single(added!.UserRoles);
        Assert.Equal(UserRoleEnum.Student, added.UserRoles.First().RoleId);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRolesAndBranchIdsProvided_AssignsThemAndSkipsMissingBranches()
    {
        var tenantId = Guid.NewGuid();
        var validBranchId = Guid.NewGuid();
        var missingBranchId = Guid.NewGuid();

        _tenants.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(AuthTestData.MakeTenant(tenantId));
        _users.ExistsByUsernameAsync("jdoe", Arg.Any<CancellationToken>()).Returns(false);
        _users.ExistsByEmailAsync("jdoe@acme.test", Arg.Any<CancellationToken>()).Returns(false);
        _hasher.HashPassword("P@ss123").Returns("hashed");
        _branches.GetByIdIgnoringFiltersAsync(validBranchId, Arg.Any<CancellationToken>())
            .Returns(new Branch { Id = validBranchId, Name = "Main", Email = "b@acme.test", CreatedOn = DateTimeOffset.UtcNow, CreatedByEmail = "a", CreatedByName = "A" });
        _branches.GetByIdIgnoringFiltersAsync(missingBranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        User? added = null;
        _users.When(r => r.Add(Arg.Any<User>())).Do(c => added = c.Arg<User>());

        var model = Model(tenantId) with
        {
            Roles = new List<UserRoleEnum> { UserRoleEnum.Coach },
            BranchIds = new List<Guid> { validBranchId, missingBranchId }
        };

        var result = await CreateSut().Handle(new RegisterCommand(model), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRoleEnum.Coach, added!.UserRoles.Single().RoleId);
        Assert.Single(added.Branches);
        Assert.Equal(validBranchId, added.Branches.Single().Id);
    }
}

public class RefreshTokenCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IAuthenticationService _authService = Substitute.For<IAuthenticationService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private RefreshTokenCommandHandler CreateSut() =>
        new(_users, _tenants, _authService, _uow, NullLogger<RefreshTokenCommandHandler>.Instance);

    [Fact]
    public async Task Handle_WhenTokenMalformed_ReturnsInvalidRefreshToken()
    {
        _authService.ValidateRefreshToken("bad-token").Returns(false);

        var result = await CreateSut().Handle(new RefreshTokenCommand(new RefreshTokenModel { RefreshToken = "bad-token" }), default);

        Assert.Equal(UserErrors.InvalidRefreshToken, result.Error);
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ReturnsInvalidRefreshToken()
    {
        _authService.ValidateRefreshToken("tok").Returns(true);
        _users.GetByRefreshTokenAsync("tok", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateSut().Handle(new RefreshTokenCommand(new RefreshTokenModel { RefreshToken = "tok" }), default);

        Assert.Equal(UserErrors.InvalidRefreshToken, result.Error);
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ReturnsRefreshTokenExpired()
    {
        var user = AuthTestData.MakeUser();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(-1);
        _authService.ValidateRefreshToken("tok").Returns(true);
        _users.GetByRefreshTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateSut().Handle(new RefreshTokenCommand(new RefreshTokenModel { RefreshToken = "tok" }), default);

        Assert.Equal(UserErrors.RefreshTokenExpired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenTenantMissing_ReturnsTenantNotFound()
    {
        var user = AuthTestData.MakeUser();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddHours(1);
        _authService.ValidateRefreshToken("tok").Returns(true);
        _users.GetByRefreshTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(user);
        _tenants.GetByIdAsync(user.TenantId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var result = await CreateSut().Handle(new RefreshTokenCommand(new RefreshTokenModel { RefreshToken = "tok" }), default);

        Assert.Equal(TenantErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_RotatesTokensAndReturnsAuthDto()
    {
        var user = AuthTestData.MakeUser();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddHours(1);
        var tenant = AuthTestData.MakeTenant(user.TenantId);
        var authResponse = AuthTestData.MakeAuthResponse(user.Id);

        _authService.ValidateRefreshToken("tok").Returns(true);
        _users.GetByRefreshTokenAsync("tok", Arg.Any<CancellationToken>()).Returns(user);
        _tenants.GetByIdAsync(user.TenantId, Arg.Any<CancellationToken>()).Returns(tenant);
        _authService.GenerateToken(user, tenant).Returns(authResponse);

        var result = await CreateSut().Handle(new RefreshTokenCommand(new RefreshTokenModel { RefreshToken = "tok" }), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("refresh-token", user.RefreshToken);
        _users.Received(1).Update(user);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class LogoutCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private LogoutCommandHandler CreateSut() => new(_users, _uow);

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsUserNotFound()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateSut().Handle(new LogoutCommand(Guid.NewGuid()), default);

        Assert.Equal(UserErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenFound_ClearsRefreshTokenAndSaves()
    {
        var user = AuthTestData.MakeUser();
        user.RefreshToken = "old-token";
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddHours(1);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateSut().Handle(new LogoutCommand(user.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Null(user.RefreshToken);
        Assert.Null(user.RefreshTokenExpiryTime);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class ChangePasswordCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ChangePasswordCommandHandler CreateSut() =>
        new(_users, _hasher, _uow, NullLogger<ChangePasswordCommandHandler>.Instance);

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsUserNotFound()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateSut().Handle(
            new ChangePasswordCommand(new ChangePasswordModel { UserId = Guid.NewGuid(), OldPassword = "old", NewPassword = "new" }), default);

        Assert.Equal(UserErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenOldPasswordIncorrect_ReturnsInvalidCredentials()
    {
        var user = AuthTestData.MakeUser();
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.VerifyPassword("wrong", user.PasswordHash).Returns(false);

        var result = await CreateSut().Handle(
            new ChangePasswordCommand(new ChangePasswordModel { UserId = user.Id, OldPassword = "wrong", NewPassword = "new" }), default);

        Assert.Equal(UserErrors.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesHashAndSaves()
    {
        var user = AuthTestData.MakeUser();
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.VerifyPassword("old", user.PasswordHash).Returns(true);
        _hasher.HashPassword("new").Returns("new-hash");

        var result = await CreateSut().Handle(
            new ChangePasswordCommand(new ChangePasswordModel { UserId = user.Id, OldPassword = "old", NewPassword = "new" }), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("new-hash", user.PasswordHash);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class ForgotPasswordCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IOtpService _otp = Substitute.For<IOtpService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ForgotPasswordCommandHandler CreateSut() =>
        new(_users, _otp, _uow, NullLogger<ForgotPasswordCommandHandler>.Instance);

    [Fact]
    public async Task Handle_WhenEmailIdentifierNotFound_ReturnsUserNotFound()
    {
        _users.GetByEmailAsync("nobody@acme.test", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateSut().Handle(
            new ForgotPasswordCommand(new ForgotPasswordModel { Identifier = "nobody@acme.test", Type = IdentifierType.Email }), default);

        Assert.Equal(UserErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenPhoneIdentifier_UsesPhoneLookup()
    {
        var user = AuthTestData.MakeUser();
        _users.GetByPhoneAsync("+1000", Arg.Any<CancellationToken>()).Returns(user);
        _otp.GenerateOtp().Returns("123456");
        _otp.SendOtpAsync("+1000", IdentifierType.Phone, "123456", Arg.Any<CancellationToken>())
            .Returns(Result.Success(OtpMessages.OtpSent));

        var result = await CreateSut().Handle(
            new ForgotPasswordCommand(new ForgotPasswordModel { Identifier = "+1000", Type = IdentifierType.Phone }), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("123456", user.PasswordResetToken);
        await _users.Received(0).GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValid_GeneratesAndSendsOtp()
    {
        var user = AuthTestData.MakeUser();
        _users.GetByEmailAsync("jdoe@acme.test", Arg.Any<CancellationToken>()).Returns(user);
        _otp.GenerateOtp().Returns("654321");
        _otp.SendOtpAsync("jdoe@acme.test", IdentifierType.Email, "654321", Arg.Any<CancellationToken>())
            .Returns(Result.Success(OtpMessages.OtpSent));

        var result = await CreateSut().Handle(
            new ForgotPasswordCommand(new ForgotPasswordModel { Identifier = "jdoe@acme.test", Type = IdentifierType.Email }), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(OtpMessages.OtpSent, result.Value);
        Assert.Equal("654321", user.PasswordResetToken);
        Assert.NotNull(user.PasswordResetTokenExpiryTime);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOtpSendFails_PropagatesFailure()
    {
        var user = AuthTestData.MakeUser();
        var sendError = new Error("Otp.SendFailed", "Could not send OTP");
        _users.GetByEmailAsync("jdoe@acme.test", Arg.Any<CancellationToken>()).Returns(user);
        _otp.GenerateOtp().Returns("654321");
        _otp.SendOtpAsync("jdoe@acme.test", IdentifierType.Email, "654321", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(sendError));

        var result = await CreateSut().Handle(
            new ForgotPasswordCommand(new ForgotPasswordModel { Identifier = "jdoe@acme.test", Type = IdentifierType.Email }), default);

        Assert.True(result.IsFailure);
        Assert.Equal(sendError, result.Error);
    }
}

public class VerifyOtpCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private VerifyOtpCommandHandler CreateSut() =>
        new(_users, _uow, NullLogger<VerifyOtpCommandHandler>.Instance);

    private static VerifyOtpModel Model(string otp = "123456") =>
        new() { Identifier = "jdoe@acme.test", Type = IdentifierType.Email, Otp = otp };

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsInvalidOrExpired()
    {
        _users.GetByEmailAsync("jdoe@acme.test", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateSut().Handle(new VerifyOtpCommand(Model()), default);

        Assert.Equal(OtpErrors.InvalidOrExpired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenOtpDoesNotMatch_ReturnsInvalidOrExpired()
    {
        var user = AuthTestData.MakeUser();
        user.PasswordResetToken = "999999";
        user.PasswordResetTokenExpiryTime = DateTime.UtcNow.AddMinutes(5);
        _users.GetByEmailAsync("jdoe@acme.test", Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateSut().Handle(new VerifyOtpCommand(Model("123456")), default);

        Assert.Equal(OtpErrors.InvalidOrExpired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenOtpExpired_ReturnsInvalidOrExpired()
    {
        var user = AuthTestData.MakeUser();
        user.PasswordResetToken = "123456";
        user.PasswordResetTokenExpiryTime = DateTime.UtcNow.AddMinutes(-1);
        _users.GetByEmailAsync("jdoe@acme.test", Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateSut().Handle(new VerifyOtpCommand(Model("123456")), default);

        Assert.Equal(OtpErrors.InvalidOrExpired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_ReplacesOtpWithResetTokenAndSaves()
    {
        var user = AuthTestData.MakeUser();
        user.PasswordResetToken = "123456";
        user.PasswordResetTokenExpiryTime = DateTime.UtcNow.AddMinutes(5);
        _users.GetByEmailAsync("jdoe@acme.test", Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateSut().Handle(new VerifyOtpCommand(Model("123456")), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(OtpMessages.OtpVerified, result.Value);
        Assert.True(Guid.TryParse(user.PasswordResetToken, out _));
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class ResetPasswordCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ResetPasswordCommandHandler CreateSut() =>
        new(_users, _hasher, _uow, NullLogger<ResetPasswordCommandHandler>.Instance);

    private static ResetPasswordModel Model(string newPassword = "N3wP@ss", string confirm = "N3wP@ss") =>
        new() { Identifier = "jdoe@acme.test", NewPassword = newPassword, ConfirmPassword = confirm };

    [Fact]
    public async Task Handle_WhenNewPasswordMissing_ReturnsPasswordRequired()
    {
        var result = await CreateSut().Handle(new ResetPasswordCommand(Model(newPassword: " ")), default);
        Assert.Equal(UserErrors.PasswordRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenPasswordsDoNotMatch_ReturnsPasswordMismatch()
    {
        var result = await CreateSut().Handle(new ResetPasswordCommand(Model(confirm: "different")), default);
        Assert.Equal(UserErrors.PasswordMismatch, result.Error);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsUserNotFound()
    {
        _users.GetByEmailAsync("jdoe@acme.test", Arg.Any<CancellationToken>()).Returns((User?)null);
        _users.GetByPhoneAsync("jdoe@acme.test", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateSut().Handle(new ResetPasswordCommand(Model()), default);

        Assert.Equal(UserErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenResetTokenNotVerified_ReturnsNotVerified()
    {
        var user = AuthTestData.MakeUser();
        user.PasswordResetToken = "123456"; // still a raw OTP, not swapped for a GUID by VerifyOtp
        user.PasswordResetTokenExpiryTime = DateTime.UtcNow.AddMinutes(5);
        _users.GetByEmailAsync("jdoe@acme.test", Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateSut().Handle(new ResetPasswordCommand(Model()), default);

        Assert.Equal(OtpErrors.NotVerified, result.Error);
    }

    [Fact]
    public async Task Handle_WhenResetTokenExpired_ReturnsNotVerified()
    {
        var user = AuthTestData.MakeUser();
        user.PasswordResetToken = Guid.NewGuid().ToString();
        user.PasswordResetTokenExpiryTime = DateTime.UtcNow.AddMinutes(-1);
        _users.GetByEmailAsync("jdoe@acme.test", Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateSut().Handle(new ResetPasswordCommand(Model()), default);

        Assert.Equal(OtpErrors.NotVerified, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_ResetsPasswordAndClearsLockoutState()
    {
        var user = AuthTestData.MakeUser();
        user.PasswordResetToken = Guid.NewGuid().ToString();
        user.PasswordResetTokenExpiryTime = DateTime.UtcNow.AddMinutes(5);
        user.FailedLoginAttempts = 3;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
        _users.GetByEmailAsync("jdoe@acme.test", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.HashPassword("N3wP@ss").Returns("new-hash");

        var result = await CreateSut().Handle(new ResetPasswordCommand(Model()), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserMessages.PasswordResetSuccessfully, result.Value);
        Assert.Equal("new-hash", user.PasswordHash);
        Assert.Null(user.PasswordResetToken);
        Assert.Null(user.PasswordResetTokenExpiryTime);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockoutEnd);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
