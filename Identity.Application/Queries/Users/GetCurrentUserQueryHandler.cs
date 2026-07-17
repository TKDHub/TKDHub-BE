using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Users
{
    public sealed record GetCurrentUserQuery(Guid userId) : IQuery<UserProfileDto>;

    internal sealed class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, UserProfileDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetCurrentUserQueryHandler> _logger;

        public GetCurrentUserQueryHandler(IUserRepository userRepository, ILogger<GetCurrentUserQueryHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Result<UserProfileDto>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetCurrentUser: looking up user {UserId}", query.userId);

            var user = await _userRepository.GetByIdAsync(query.userId, cancellationToken);
            if (user is null)
            {
                _logger.LogInformation("GetCurrentUser: user {UserId} not found", query.userId);
                return Result.Failure<UserProfileDto>(UserErrors.UserNotFound);
            }

            _logger.LogInformation("GetCurrentUser: found user {UserId}", user.Id);
            return Result.Success(user.ToProfileDto());
        }
    }
}
