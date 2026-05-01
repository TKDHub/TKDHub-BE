using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Tenants
{
    public sealed record DeleteTenantCommand(Guid TenantId) : ICommand;

    internal sealed class DeleteTenantCommandHandler : ICommandHandler<DeleteTenantCommand>
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteTenantCommandHandler> _logger;

        public DeleteTenantCommandHandler(
            ITenantRepository tenantRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeleteTenantCommandHandler> logger)
        {
            _tenantRepository = tenantRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
        {
            var tenant = await _tenantRepository.GetByIdIgnoringFiltersAsync(request.TenantId, cancellationToken);
            if (tenant is null)
                return Result.Failure(TenantErrors.NotFound);

            var activeUsers = await _userRepository.GetByTenantIdAsync(request.TenantId, cancellationToken);
            if (activeUsers.Count > 0)
                return Result.Failure(TenantErrors.HasActiveUsers);

            tenant.StatusId = (short)EntityStatusEnum.Inactive;
            tenant.ModifiedOn = DateTimeOffset.UtcNow;

            _tenantRepository.Remove(tenant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tenant {TenantId} deleted successfully", request.TenantId);

            return Result.Success();
        }
    }
}
