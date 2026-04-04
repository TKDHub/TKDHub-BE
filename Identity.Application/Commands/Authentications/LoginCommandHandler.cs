using Identity.Application.Contracts;
using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Application.Models.Auth;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
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

        public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IAuthenticationService authenticationService, IUnitOfWork unitOfWork, ITenantRepository tenantRepository)
        {
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
            _passwordHasher = passwordHasher;
            _authenticationService = authenticationService;
            _unitOfWork = unitOfWork;
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

                return Result.Failure<AuthDto>(UserErrors.InvalidCredentials);
            }

            var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken);
            if (tenant is null)
            {
                return Result.Failure<AuthDto>(TenantErrors.NotFound);
            }
            // Generate tokens
            var authenticationResponse = _authenticationService.GenerateToken(user, tenant);

            // Update user
            RecordSuccessfulLogin(user);

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Create response
            var response = user.ToAuthDto(authenticationResponse.AccessToken, authenticationResponse.RefreshToken, authenticationResponse.ExpiresAt);

            return Result.Success(response);
        }

        public Result RecordFailedLoginAttempt(User user)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
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
