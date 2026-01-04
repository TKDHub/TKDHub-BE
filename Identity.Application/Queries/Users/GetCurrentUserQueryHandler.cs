using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Users
{
    public sealed record GetCurrentUserQuery(Guid userId) : IQuery<UserProfileDto>;

    internal sealed class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, UserProfileDto>
    {
        private readonly IUserRepository _userRepository;

        public GetCurrentUserQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<UserProfileDto>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(query.userId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<UserProfileDto>(UserErrors.UserNotFound);
            }

            return Result.Success(user.ToProfileDto());
        }
    }
}
