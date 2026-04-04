using Identity.Application.Dtos.Tenants;
using Identity.Application.Mappings.Tenants;
using Identity.Application.Models.Tenant;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Tenants
{
    public sealed record UpdateTenantCommand(TenantModel model) : ICommand<TenantDto>;

    internal sealed class UpdateTenantCommandHandler : ICommandHandler<UpdateTenantCommand, TenantDto>
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTenantCommandHandler(ITenantRepository tenantRepository, IUnitOfWork unitOfWork)
        {
            _tenantRepository = tenantRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<TenantDto>> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.model.Name))
            {
                return Result.Failure<TenantDto>(TenantErrors.NameRequired);
            }

            if (string.IsNullOrWhiteSpace(request.model.ContactEmail))
            {
                return Result.Failure<TenantDto>(TenantErrors.EmailRequired);
            }

            var tenant = await _tenantRepository.GetByIdAsync(request.model.TenentId, cancellationToken);
            if (tenant is null)
            {
                return Result.Failure<TenantDto>(TenantErrors.NotFound);
            }

            tenant.Name = request.model.Name;
            tenant.ContactEmail = request.model.ContactEmail;
            tenant.ModifiedOn = DateTimeOffset.UtcNow;

            _tenantRepository.Update(tenant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(tenant.ToDto());
        }
    }
}
