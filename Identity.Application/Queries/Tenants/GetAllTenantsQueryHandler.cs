using Identity.Application.Dtos.Tenants;
using Identity.Application.Mappings.Tenants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Tenants
{
    public sealed record GetAllTenantsQuery() : IQuery<List<TenantDto>>;

    internal sealed class GetAllTenantsQueryHandler : IQueryHandler<GetAllTenantsQuery, List<TenantDto>>
    {
        private readonly ITenantRepository _tenantRepository;

        public GetAllTenantsQueryHandler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<Result<List<TenantDto>>> Handle(GetAllTenantsQuery query, CancellationToken cancellationToken)
        {
            var tenants = await _tenantRepository.GetAllAsync(cancellationToken);

            return Result.Success(tenants.ToListDtos());
        }
    }
}
