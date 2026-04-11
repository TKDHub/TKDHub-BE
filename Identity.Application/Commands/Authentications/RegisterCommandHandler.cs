using Identity.Application.Contracts;
using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Application.Models.User;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
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
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RegisterCommandHandler> _logger;

        public RegisterCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork, ITenantRepository tenantRepository, ILogger<RegisterCommandHandler> logger)
        {
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<RegisterDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            // Validate inputs before hitting the database
            if (string.IsNullOrWhiteSpace(request.model.Email))
            {
                return Result.Failure<RegisterDto>(UserErrors.EmailRequired);
            }

            if (!IsValidEmail(request.model.Email))
            {
                return Result.Failure<RegisterDto>(UserErrors.InvalidEmailFormat);
            }

            if (string.IsNullOrWhiteSpace(request.model.FirstName))
            {
                return Result.Failure<RegisterDto>(UserErrors.FirstNameRequired);
            }

            if (string.IsNullOrWhiteSpace(request.model.LastName))
            {
                return Result.Failure<RegisterDto>(UserErrors.LastNameRequired);
            }

            // Normalize email before storing
            var normalizedEmail = request.model.Email.Trim().ToLowerInvariant();

            // Verify tenant exists
            var tenant = await _tenantRepository.GetByIdAsync(request.model.TenantId, cancellationToken);

            if (tenant is null)
            {
                return Result.Failure<RegisterDto>(TenantErrors.NotFound);
            }

            // Check if user already exists
            var existingUser = await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken);

            if (existingUser)
            {
                return Result.Failure<RegisterDto>(UserErrors.EmailAlreadyExists);
            }

            // Hash password
            var passwordHash = _passwordHasher.HashPassword(request.model.Password);
            request.model.PasswordHash = passwordHash;

            var user = request.model.ToEntity();

            // Add default role
            AddRole(user, UserRoles.Default);

            // Save user
            _userRepository.Add(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("New user registered with email {Email} under tenant {TenantId}", normalizedEmail, request.model.TenantId);

            return Result.Success(user.ToDto());
        }

        public bool IsValidEmail(string email)
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

        public Result AddRole(User user, string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return Result.Failure(UserErrors.InvalidRole);
            }

            if (user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                return Result.Failure(UserErrors.RoleAlreadyAssigned);
            }

            user.AddRoleInternal(role);
            return Result.Success();
        }
    }
}