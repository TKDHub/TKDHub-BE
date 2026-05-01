using Identity.Application.Contracts;
using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Tenants;
using Identity.Application.Mappings.Users;
using Identity.Application.Models.User;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Authentications
{
    public sealed record RegisterCommand(RegisterUserModel model) : ICommand<RegisterDto>;

    internal sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand, RegisterDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RegisterCommandHandler> _logger;

        public RegisterCommandHandler(
            IUserRepository userRepository,
            ITenantRepository tenantRepository,
            IBranchRepository branchRepository,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork,
            ILogger<RegisterCommandHandler> logger)
        {
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
            _branchRepository = branchRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<RegisterDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.model.Username))
                return Result.Failure<RegisterDto>(UserErrors.UsernameRequired);

            if (string.IsNullOrWhiteSpace(request.model.Email))
                return Result.Failure<RegisterDto>(UserErrors.EmailRequired);

            if (!IsValidEmail(request.model.Email))
                return Result.Failure<RegisterDto>(UserErrors.InvalidEmailFormat);

            var tenant = await _tenantRepository.GetByIdAsync(request.model.TenantId, cancellationToken);
            if (tenant is null)
                return Result.Failure<RegisterDto>(TenantErrors.NotFound);

            var usernameExists = await _userRepository.ExistsByUsernameAsync(request.model.Username, cancellationToken);
            if (usernameExists)
                return Result.Failure<RegisterDto>(UserErrors.UsernameAlreadyExists);

            var emailExists = await _userRepository.ExistsByEmailAsync(request.model.Email.Trim().ToLowerInvariant(), cancellationToken);
            if (emailExists)
                return Result.Failure<RegisterDto>(UserErrors.EmailAlreadyExists);

            request.model.PasswordHash = _passwordHasher.HashPassword(request.model.Password);

            var user = request.model.ToEntity();

            var roles = request.model.Roles.Count > 0
                ? request.model.Roles
                : new List<UserRoleEnum> { UserRoleEnum.Student };

            foreach (var roleId in roles)
                user.UserRoles.Add(new UserRole { RoleId = roleId });

            if (request.model.BranchIds.Count > 0)
            {
                foreach (var branchId in request.model.BranchIds)
                {
                    var branch = await _branchRepository.GetByIdAsync(branchId, cancellationToken);
                    if (branch is not null)
                        user.Branches.Add(branch);
                }
            }

            _userRepository.Add(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("New user registered: {Username} under tenant {TenantId}", user.Username, user.TenantId);

            return Result.Success(user.ToDto(tenant.ToDto()));
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
