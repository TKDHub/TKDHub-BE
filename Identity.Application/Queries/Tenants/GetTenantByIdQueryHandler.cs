using Identity.Application.Dtos.Tenants;
using Identity.Application.Mappings.Tenants;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Tenants
{
    public sealed record GetTenantByIdQuery(Guid TenantId) : IQuery<TenantDto>;

    internal sealed class GetTenantByIdQueryHandler : IQueryHandler<GetTenantByIdQuery, TenantDto>
    {
        private readonly ITenantRepository _tenantRepository;

        public GetTenantByIdQueryHandler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<Result<TenantDto>> Handle(GetTenantByIdQuery query, CancellationToken cancellationToken)
        {
            var tenant = await _tenantRepository.GetByIdAsync(query.TenantId, cancellationToken);
            if (tenant is null)
            {
                return Result.Failure<TenantDto>(TenantErrors.NotFound);
            }

            return Result.Success(tenant.ToDto());
        }
    }
}
