using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Authentications
{
    public sealed record LogoutCommand(Guid UserId) : ICommand;

    internal sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LogoutCommandHandler> _logger;

        public LogoutCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, ILogger<LogoutCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Logout: starting for user {UserId}", request.UserId);

            // Get user
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

            if (user is null)
            {
                _logger.LogInformation("Logout: user {UserId} not found", request.UserId);
                return Result.Failure(UserErrors.UserNotFound);
            }

            // Clear refresh token
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            // Save changes
            _userRepository.Update(user);

            _logger.LogInformation("Logout: saving changes");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Logout: succeeded — user {UserId} logged out", user.Id);
            return Result.Success();
        }
    }
}
