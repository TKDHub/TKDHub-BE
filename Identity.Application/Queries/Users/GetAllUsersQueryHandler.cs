using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Users
{
    public sealed record GetAllUsersQuery(PagedRequest Request) : IQuery<PagedResult<UserProfileDto>>;

    internal sealed class GetAllUsersQueryHandler : IQueryHandler<GetAllUsersQuery, PagedResult<UserProfileDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetAllUsersQueryHandler> _logger;

        public GetAllUsersQueryHandler(IUserRepository userRepository, ILogger<GetAllUsersQueryHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Result<PagedResult<UserProfileDto>>> Handle(GetAllUsersQuery query, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetAllUsers: querying page {Page} size {PageSize}", query.Request.Page, query.Request.PageSize);

            var result = await _userRepository.GetPagedAsync(query.Request, cancellationToken);

            _logger.LogInformation("GetAllUsers: returned {Count} of {Total} user(s)", result.Items.Count, result.TotalCount);
            return Result.Success(PagedResult<UserProfileDto>.Create(
                result.Items.ToListModels(),
                result.TotalCount,
                result.Page,
                result.PageSize));
        }
    }
}
