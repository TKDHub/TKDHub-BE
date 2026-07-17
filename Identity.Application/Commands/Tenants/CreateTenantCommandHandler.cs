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
    public sealed record CreateTenantCommand(TenantModel model) : ICommand<TenantDto>;

    internal sealed class CreateTenantCommandHandler : ICommandHandler<CreateTenantCommand, TenantDto>
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateTenantCommandHandler> _logger;

        public CreateTenantCommandHandler(ITenantRepository tenantRepository, IUnitOfWork unitOfWork, ILogger<CreateTenantCommandHandler> logger)
        {
            _tenantRepository = tenantRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<TenantDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("CreateTenant: starting for name {Name}", request.model.Name);

            if (string.IsNullOrWhiteSpace(request.model.Name))
            {
                _logger.LogInformation("CreateTenant: rejected — name missing");
                return Result.Failure<TenantDto>(TenantErrors.NameRequired);
            }

            if (string.IsNullOrWhiteSpace(request.model.Subdomain))
            {
                _logger.LogInformation("CreateTenant: rejected — subdomain missing");
                return Result.Failure<TenantDto>(TenantErrors.SubdomainRequired);
            }

            if (string.IsNullOrWhiteSpace(request.model.ContactEmail))
            {
                _logger.LogInformation("CreateTenant: rejected — contact email missing");
                return Result.Failure<TenantDto>(TenantErrors.EmailRequired);
            }

            // Check subdomain exists
            var exists = await _tenantRepository.ExistsBySubdomainAsync(request.model.Subdomain, cancellationToken);

            if (exists)
            {
                _logger.LogInformation("CreateTenant: rejected — subdomain {Subdomain} already exists", request.model.Subdomain);
                return Result.Failure<TenantDto>(TenantErrors.SubdomainExists);
            }

            var tenant = request.model.ToEntity();

            _logger.LogInformation("CreateTenant: adding tenant entity");
            _tenantRepository.Add(tenant);

            _logger.LogInformation("CreateTenant: saving changes");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("CreateTenant: succeeded — tenant {TenantId} created", tenant.Id);
            return Result.Success(tenant.ToDto());
        }
    }
}
