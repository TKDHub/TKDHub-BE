using Identity.Application.Contracts;
using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Authentications
{
    public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<AuthDto>;

    internal sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticationService _authenticationService;
        private readonly IUnitOfWork _unitOfWork;

        public RefreshTokenCommandHandler(IUserRepository userRepository, IAuthenticationService authenticationService, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _authenticationService = authenticationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<AuthDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // Find user by refresh token
            var users = await _userRepository.GetAllAsync(cancellationToken);
            var user = users.FirstOrDefault(u => u.RefreshToken == request.RefreshToken);

            if (user is null)
            {
                return Result.Failure<AuthDto>(UserErrors.InvalidRefreshToken);
            }

            // Validate refresh token expiry
            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Result.Failure<AuthDto>(UserErrors.RefreshTokenExpired);
            }

            // Generate new tokens
            var authenticationResponse = _authenticationService.GenerateToken(user, null);

            // Update user
            user.RefreshToken = authenticationResponse.RefreshToken;
            user.RefreshTokenExpiryTime = authenticationResponse.ExpiresAt;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(user.ToAuthDto(authenticationResponse.AccessToken, authenticationResponse.RefreshToken, authenticationResponse.ExpiresAt));
        }
    }
}