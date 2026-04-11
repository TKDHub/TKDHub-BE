using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Users
{
    public sealed record GetAllUsersQuery(PagedRequest Request) : IQuery<PagedResult<UserProfileDto>>;

    internal sealed class GetAllUsersQueryHandler : IQueryHandler<GetAllUsersQuery, PagedResult<UserProfileDto>>
    {
        private readonly IUserRepository _userRepository;

        public GetAllUsersQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<PagedResult<UserProfileDto>>> Handle(GetAllUsersQuery query, CancellationToken cancellationToken)
        {
            var result = await _userRepository.GetPagedAsync(query.Request, cancellationToken);

            return Result.Success(PagedResult<UserProfileDto>.Create(
                result.Items.ToListModels(),
                result.TotalCount,
                result.Page,
                result.PageSize));
        }
    }
}
