using Identity.Application.Dtos.Tenants;
using Identity.Application.Mappings.Tenants;
using Identity.Application.Models.Tenant;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Tenants
{
    public sealed record CreateTenantCommand(TenantModel model) : ICommand<TenantDto>;

    internal sealed class CreateTenantCommandHandler : ICommandHandler<CreateTenantCommand, TenantDto>
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateTenantCommandHandler(ITenantRepository tenantRepository, IUnitOfWork unitOfWork)
        {
            _tenantRepository = tenantRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<TenantDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.model.Name))
            {
                return Result.Failure<TenantDto>(TenantErrors.NameRequired);
            }

            if (string.IsNullOrWhiteSpace(request.model.Subdomain))
            {
                return Result.Failure<TenantDto>(TenantErrors.SubdomainRequired);
            }

            if (string.IsNullOrWhiteSpace(request.model.ContactEmail))
            {
                return Result.Failure<TenantDto>(TenantErrors.EmailRequired);
            }

            // Check subdomain exists
            var exists = await _tenantRepository.ExistsBySubdomainAsync(request.model.Subdomain, cancellationToken);

            if (exists)
            {
                return Result.Failure<TenantDto>(TenantErrors.SubdomainExists);
            }

            var tenant = request.model.ToEntity();

            _tenantRepository.Add(tenant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(tenant.ToDto());
        }
    }
}
