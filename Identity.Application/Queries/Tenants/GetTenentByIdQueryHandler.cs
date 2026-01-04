using Identity.Application.Dtos.Tenants;
using Identity.Application.Mappings.Tenants;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Tenants
{
    public sealed record GetTenentByIdQuery(Guid tenentId) : IQuery<TenantDto>;

    internal sealed class GetTenentByIdQueryHandler : IQueryHandler<GetTenentByIdQuery, TenantDto>
    {
        private readonly ITenantRepository _tenantRepository;

        public GetTenentByIdQueryHandler( ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<Result<TenantDto>> Handle(GetTenentByIdQuery query, CancellationToken cancellationToken)
        {
            var tenant = await _tenantRepository.GetByIdAsync(query.tenentId, cancellationToken);
            if (tenant is null)
            {
                return Result.Failure<TenantDto>(TenantErrors.NotFound);
            }

            return Result.Success(tenant.ToDto());
        }
    }
}
