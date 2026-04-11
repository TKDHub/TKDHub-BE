using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Application.Models.User;
using Identity.Domain.Constants;
using Identity.Domain.Enums;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Users
{
    public sealed record UpdateAccountCommand(Guid UserId, UpdateAccountModel model) : ICommand<UserProfileDto>;

    internal sealed class UpdateAccountCommandHandler : ICommandHandler<UpdateAccountCommand, UserProfileDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateAccountCommandHandler> _logger;

        public UpdateAccountCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateAccountCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<UserProfileDto>> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
                return Result.Failure<UserProfileDto>(UserErrors.UserNotFound);

            // Update email if provided and changed
            if (!string.IsNullOrWhiteSpace(request.model.Email))
            {
                var normalizedEmail = request.model.Email.Trim().ToLowerInvariant();
                if (!normalizedEmail.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var emailTaken = await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken);
                    if (emailTaken)
                        return Result.Failure<UserProfileDto>(UserErrors.EmailAlreadyExists);

                    user.Email = normalizedEmail;
                }
            }

            // Update active status
            if (request.model.Active.HasValue)
            {
                user.StatusId = request.model.Active.Value
                    ? (short)EntityStatusEnum.Active
                    : (short)EntityStatusEnum.Inactive;
            }

            // Update phone number
            if (request.model.PhoneNumber is not null)
                user.PhoneNumber = request.model.PhoneNumber.Trim();

            // Update role based on actor type
            if (request.model.Actor.HasValue)
            {
                var roleName = request.model.Actor.Value.ToString();
                if (!user.Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
                {
                    user.RemoveRoleInternal(user.Roles.FirstOrDefault() ?? string.Empty);
                    user.AddRoleInternal(roleName);
                }
            }

            user.ModifiedOn = DateTimeOffset.UtcNow;
            user.ModifiedByEmail = request.model.ModifiedByEmail;
            user.ModifiedByName = request.model.ModifiedByName;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Account {UserId} updated successfully", user.Id);

            return Result.Success(user.ToProfileDto());
        }
    }
}
