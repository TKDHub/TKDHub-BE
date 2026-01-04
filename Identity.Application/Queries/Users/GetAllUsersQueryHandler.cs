using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Users
{
    public sealed record GetAllUsersQuery() : IQuery<List<UserProfileDto>>;

    internal sealed class GetAllUsersQueryHandler : IQueryHandler<GetAllUsersQuery, List<UserProfileDto>>
    {
        private readonly IUserRepository _userRepository;

        public GetAllUsersQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<List<UserProfileDto>>> Handle(GetAllUsersQuery query, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetAllAsync(cancellationToken);

            return Result.Success(users.ToListModels());
        }
    }
}
