using Identity.Application.Dtos.Tenants;
using Identity.Application.Mappings.Tenants;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Tenants
{
    public sealed record GetTenantBySubdomainQuery(string Subdomain) : IQuery<TenantDto>;

    internal sealed class GetTenantBySubdomainQueryHandler : IQueryHandler<GetTenantBySubdomainQuery, TenantDto>
    {
        private readonly ITenantRepository _tenantRepository;

        public GetTenantBySubdomainQueryHandler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<Result<TenantDto>> Handle(GetTenantBySubdomainQuery query, CancellationToken cancellationToken)
        {
            var tenant = await _tenantRepository.GetBySubdomainAsync(query.Subdomain, cancellationToken);

            if (tenant is null)
            {
                return Result.Failure<TenantDto>(TenantErrors.NotFound);
            }

            return Result.Success(tenant.ToDto());
        }
    }
}
