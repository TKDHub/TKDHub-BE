using Identity.Application.Contracts;
using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Application.Models.Auth;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Authentications
{
    public sealed record LoginCommand(AuthModel model) : ICommand<AuthDto>;

    internal sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthenticationService _authenticationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IAuthenticationService authenticationService, IUnitOfWork unitOfWork, ITenantRepository tenantRepository, ILogger<LoginCommandHandler> logger)
        {
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
            _passwordHasher = passwordHasher;
            _authenticationService = authenticationService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<AuthDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // Get user by email
            var user = await _userRepository.GetByEmailAsync(request.model.Email, cancellationToken);

            if (user is null)
            {
                return Result.Failure<AuthDto>(UserErrors.InvalidCredentials);
            }

            // Check if account is active
            if (user.StatusId != (short)EntityStatusEnum.Active)
            {
                return Result.Failure<AuthDto>(UserErrors.AccountNotActive);
            }

            // Check if account is locked
            if (IsLockedOut(user))
            {
                return Result.Failure<AuthDto>(UserErrors.AccountLockedOut);
            }

            // Verify password
            if (!_passwordHasher.VerifyPassword(request.model.Password, user.PasswordHash))
            {
                RecordFailedLoginAttempt(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("Failed login attempt for email {Email}. Failed attempts: {Count}", request.model.Email, user.FailedLoginAttempts);
                return Result.Failure<AuthDto>(UserErrors.InvalidCredentials);
            }

            var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken);
            if (tenant is null)
            {
                _logger.LogError("Tenant {TenantId} not found for user {UserId} during login", user.TenantId, user.Id);
                return Result.Failure<AuthDto>(TenantErrors.NotFound);
            }

            // Generate tokens
            var authenticationResponse = _authenticationService.GenerateToken(user, tenant);

            // Update user
            RecordSuccessfulLogin(user);
            user.RefreshToken = authenticationResponse.RefreshToken;
            user.RefreshTokenExpiryTime = authenticationResponse.ExpiresAt;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

            // Create response
            var response = user.ToAuthDto(authenticationResponse.AccessToken, authenticationResponse.RefreshToken, authenticationResponse.ExpiresAt);

            return Result.Success(response);
        }

        public Result RecordFailedLoginAttempt(User user)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= LoginPolicy.MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LoginPolicy.LockoutDurationMinutes);
                return Result.Failure(UserErrors.AccountLockedOut);
            }

            return Result.Success();
        }

        public bool IsLockedOut(User user)
        {
            return user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow;
        }

        public void RecordSuccessfulLogin(User user)
        {
            user.LastLoginDate = DateTime.UtcNow;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
        }
    }
}
