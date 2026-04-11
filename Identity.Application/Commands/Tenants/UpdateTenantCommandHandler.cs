using Identity.Application.Dtos.Tenants;
using Identity.Application.Mappings.Tenants;
using Identity.Application.Models.Tenant;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Tenants
{
    public sealed record UpdateTenantCommand(TenantModel model) : ICommand<TenantDto>;

    internal sealed class UpdateTenantCommandHandler : ICommandHandler<UpdateTenantCommand, TenantDto>
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateTenantCommandHandler> _logger;

        public UpdateTenantCommandHandler(ITenantRepository tenantRepository, IUnitOfWork unitOfWork, ILogger<UpdateTenantCommandHandler> logger)
        {
            _tenantRepository = tenantRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<TenantDto>> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.model.Name))
                return Result.Failure<TenantDto>(TenantErrors.NameRequired);

            if (string.IsNullOrWhiteSpace(request.model.ContactEmail))
                return Result.Failure<TenantDto>(TenantErrors.EmailRequired);

            var tenant = await _tenantRepository.GetByIdAsync(request.model.TenantId, cancellationToken);
            if (tenant is null)
                return Result.Failure<TenantDto>(TenantErrors.NotFound);

            // Check for subdomain conflict only if subdomain is being changed
            var normalizedSubdomain = request.model.Subdomain.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(normalizedSubdomain) && normalizedSubdomain != tenant.Subdomain)
            {
                var subdomainTaken = await _tenantRepository.ExistsBySubdomainAsync(normalizedSubdomain, cancellationToken);
                if (subdomainTaken)
                {
                    _logger.LogWarning("Subdomain '{Subdomain}' is already in use — update rejected for tenant {TenantId}", normalizedSubdomain, tenant.Id);
                    return Result.Failure<TenantDto>(TenantErrors.SubdomainExists);
                }

                tenant.Subdomain = normalizedSubdomain;
            }

            tenant.Name = request.model.Name.Trim();
            tenant.ContactEmail = request.model.ContactEmail.Trim().ToLowerInvariant();
            tenant.ModifiedOn = DateTimeOffset.UtcNow;

            _tenantRepository.Update(tenant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tenant {TenantId} updated successfully", tenant.Id);

            return Result.Success(tenant.ToDto());
        }
    }
}
