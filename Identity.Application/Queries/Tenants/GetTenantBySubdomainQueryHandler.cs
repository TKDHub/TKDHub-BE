using Identity.Application.Dtos.Tenants;
using Identity.Application.Mappings.Tenants;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Tenants
{
    public sealed record GetTenantBySubdomainQuery(string Subdomain) : IQuery<TenantDto>;

    internal sealed class GetTenantBySubdomainQueryHandler : IQueryHandler<GetTenantBySubdomainQuery, TenantDto>
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ILogger<GetTenantBySubdomainQueryHandler> _logger;

        public GetTenantBySubdomainQueryHandler(ITenantRepository tenantRepository, ILogger<GetTenantBySubdomainQueryHandler> logger)
        {
            _tenantRepository = tenantRepository;
            _logger = logger;
        }

        public async Task<Result<TenantDto>> Handle(GetTenantBySubdomainQuery query, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetTenantBySubdomain: looking up subdomain {Subdomain}", query.Subdomain);

            var tenant = await _tenantRepository.GetBySubdomainAsync(query.Subdomain, cancellationToken);

            if (tenant is null)
            {
                _logger.LogInformation("GetTenantBySubdomain: subdomain {Subdomain} not found", query.Subdomain);
                return Result.Failure<TenantDto>(TenantErrors.NotFound);
            }

            _logger.LogInformation("GetTenantBySubdomain: found tenant {TenantId}", tenant.Id);
            return Result.Success(tenant.ToDto());
        }
    }
}
