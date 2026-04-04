using Identity.Application.Dtos.Users;
using Identity.Application.Models.User;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Shared.Domain.Enums;

namespace Identity.Application.Mappings.Users
{
    public static class UserMappings
    {
        public static User ToEntity(this RegisterUserModel model)
        {
            return new User
            {
                TenantId = model.TenantId,
                Email = model.Email.ToLowerInvariant(),
                FirstName = model.FirstName,
                LastName = model.LastName,
                PasswordHash = model.PasswordHash,
                EmailConfirmed = true,
                FailedLoginAttempts = 0,
                CreatedOn = DateTime.UtcNow,
            };
        }

        public static UserModel ToModel(this User user)
        {
            return new UserModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Status = (EntityStatusEnum)user.StatusId,
                EmailConfirmed = user.EmailConfirmed,
                LastLoginDate = user.LastLoginDate,
                CreatedOn = user.CreatedOn.UtcDateTime,
                ModifiedOn = user.ModifiedOn?.UtcDateTime,
                TenantId = user.TenantId
            };
        }

        /// <summary>
        /// Convert User entity to UserProfileDto
        /// </summary>
        public static UserProfileDto ToProfileDto(this User user)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Status = (EntityStatusEnum)user.StatusId,
                EmailConfirmed = user.EmailConfirmed,
                LastLoginDate = user.LastLoginDate.HasValue ? new DateTimeOffset(user.LastLoginDate.Value, TimeSpan.Zero) : null,
                CreatedOn = user.CreatedOn,
                ModifiedOn = user.ModifiedOn,
                TenantId = user.TenantId
            };
        }

        /// <summary>
        /// Convert list of users to list of UserModel
        /// </summary>
        public static List<UserProfileDto> ToListModels(this IEnumerable<User> users)
        {
            return users.Select(u => u.ToProfileDto()).ToList();
        }

        // ===== Model → DTO (Input) =====

        /// <summary>
        /// Convert User to RegisterDto
        /// </summary>
        public static RegisterDto ToDto(this User user)
        {
            return new RegisterDto
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Message = UserMessages.UserRegisteredSuccessfully,
            };
        }
    }
}
