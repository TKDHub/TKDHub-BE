using Identity.Application.Dtos.Tenants;
using Identity.Application.Mappings.Tenants;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Tenants
{
    public sealed record GetTenantByIdQuery(Guid TenantId) : IQuery<TenantDto>;

    internal sealed class GetTenantByIdQueryHandler : IQueryHandler<GetTenantByIdQuery, TenantDto>
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ILogger<GetTenantByIdQueryHandler> _logger;

        public GetTenantByIdQueryHandler(ITenantRepository tenantRepository, ILogger<GetTenantByIdQueryHandler> logger)
        {
            _tenantRepository = tenantRepository;
            _logger = logger;
        }

        public async Task<Result<TenantDto>> Handle(GetTenantByIdQuery query, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetTenantById: looking up tenant {TenantId}", query.TenantId);

            var tenant = await _tenantRepository.GetByIdAsync(query.TenantId, cancellationToken);
            if (tenant is null)
            {
                _logger.LogInformation("GetTenantById: tenant {TenantId} not found", query.TenantId);
                return Result.Failure<TenantDto>(TenantErrors.NotFound);
            }

            _logger.LogInformation("GetTenantById: found tenant {TenantId}", tenant.Id);
            return Result.Success(tenant.ToDto());
        }
    }
}
