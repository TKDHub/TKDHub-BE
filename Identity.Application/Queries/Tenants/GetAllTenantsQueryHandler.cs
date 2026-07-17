using Identity.Application.Dtos.Tenants;
using Identity.Application.Mappings.Tenants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Tenants
{
    public sealed record GetAllTenantsQuery(PagedRequest Request) : IQuery<PagedResult<TenantDto>>;

    internal sealed class GetAllTenantsQueryHandler : IQueryHandler<GetAllTenantsQuery, PagedResult<TenantDto>>
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ILogger<GetAllTenantsQueryHandler> _logger;

        public GetAllTenantsQueryHandler(ITenantRepository tenantRepository, ILogger<GetAllTenantsQueryHandler> logger)
        {
            _tenantRepository = tenantRepository;
            _logger = logger;
        }

        public async Task<Result<PagedResult<TenantDto>>> Handle(GetAllTenantsQuery query, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetAllTenants: querying page {Page} size {PageSize}", query.Request.Page, query.Request.PageSize);

            var result = await _tenantRepository.GetPagedAsync(query.Request, cancellationToken);

            _logger.LogInformation("GetAllTenants: returned {Count} of {Total} tenant(s)", result.Items.Count, result.TotalCount);
            return Result.Success(PagedResult<TenantDto>.Create(
                result.Items.ToListDtos(),
                result.TotalCount,
                result.Page,
                result.PageSize));
        }
    }
}
