using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Users
{
    public sealed record GetUserByIdQuery(Guid userId) : IQuery<UserProfileDto>;

    internal sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserProfileDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUserByIdQueryHandler> _logger;

        public GetUserByIdQueryHandler(IUserRepository userRepository, ILogger<GetUserByIdQueryHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Result<UserProfileDto>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetUserById: looking up user {UserId}", query.userId);

            var user = await _userRepository.GetByIdAsync(query.userId, cancellationToken);
            if (user is null)
            {
                _logger.LogInformation("GetUserById: user {UserId} not found", query.userId);
                return Result.Failure<UserProfileDto>(UserErrors.UserNotFound);
            }

            _logger.LogInformation("GetUserById: found user {UserId}", user.Id);
            return Result.Success(user.ToProfileDto());
        }
    }
}
