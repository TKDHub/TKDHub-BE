using Identity.Application.Dtos.Tenants;
using Identity.Application.Mappings.Tenants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Tenants
{
    public sealed record GetAllTenantsQuery(PagedRequest Request) : IQuery<PagedResult<TenantDto>>;

    internal sealed class GetAllTenantsQueryHandler : IQueryHandler<GetAllTenantsQuery, PagedResult<TenantDto>>
    {
        private readonly ITenantRepository _tenantRepository;

        public GetAllTenantsQueryHandler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<Result<PagedResult<TenantDto>>> Handle(GetAllTenantsQuery query, CancellationToken cancellationToken)
        {
            var result = await _tenantRepository.GetPagedAsync(query.Request, cancellationToken);

            return Result.Success(PagedResult<TenantDto>.Create(
                result.Items.ToListDtos(),
                result.TotalCount,
                result.Page,
                result.PageSize));
        }
    }
}
