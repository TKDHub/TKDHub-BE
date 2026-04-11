using Identity.Application.Contracts;
using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Authentications
{
    public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<AuthDto>;

    internal sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IAuthenticationService _authenticationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;

        public RefreshTokenCommandHandler(IUserRepository userRepository, ITenantRepository tenantRepository, IAuthenticationService authenticationService, IUnitOfWork unitOfWork, ILogger<RefreshTokenCommandHandler> logger)
        {
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
            _authenticationService = authenticationService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<AuthDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // Validate token format before hitting the database
            if (!_authenticationService.ValidateRefreshToken(request.RefreshToken))
            {
                _logger.LogWarning("Refresh token attempt with malformed token");
                return Result.Failure<AuthDto>(UserErrors.InvalidRefreshToken);
            }

            // Find user by refresh token
            var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);

            if (user is null)
            {
                _logger.LogWarning("Refresh token attempt with invalid token");
                return Result.Failure<AuthDto>(UserErrors.InvalidRefreshToken);
            }

            // Validate refresh token expiry
            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Expired refresh token used for user {UserId}", user.Id);
                return Result.Failure<AuthDto>(UserErrors.RefreshTokenExpired);
            }

            // Load tenant for JWT claims
            var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken);
            if (tenant is null)
            {
                _logger.LogError("Tenant {TenantId} not found for user {UserId} during token refresh", user.TenantId, user.Id);
                return Result.Failure<AuthDto>(TenantErrors.NotFound);
            }

            // Generate new tokens
            var authenticationResponse = _authenticationService.GenerateToken(user, tenant);

            // Update user
            user.RefreshToken = authenticationResponse.RefreshToken;
            user.RefreshTokenExpiryTime = authenticationResponse.ExpiresAt;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Token refreshed successfully for user {UserId}", user.Id);

            return Result.Success(user.ToAuthDto(authenticationResponse.AccessToken, authenticationResponse.RefreshToken, authenticationResponse.ExpiresAt));
        }
    }
}
