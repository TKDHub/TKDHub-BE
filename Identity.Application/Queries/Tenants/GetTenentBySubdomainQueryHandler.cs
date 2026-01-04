using Identity.Application.Dtos.Tenants;
using Identity.Application.Mappings.Tenants;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Tenants
{
    public sealed record GetTenentBySubdomainQuery(string subdomain) : IQuery<TenantDto>;

    internal sealed class GetTenentBySubdomainQueryHandler : IQueryHandler<GetTenentBySubdomainQuery, TenantDto>
    {
        private readonly ITenantRepository _tenantRepository;

        public GetTenentBySubdomainQueryHandler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<Result<TenantDto>> Handle(GetTenentBySubdomainQuery query, CancellationToken cancellationToken)
        {
            var tenant = await _tenantRepository.GetBySubdomainAsync(query.subdomain, cancellationToken);

            if (tenant is null)
            {
                return Result.Failure<TenantDto>(TenantErrors.NotFound);
            }

            return Result.Success(tenant.ToDto());
        }
    }
}
