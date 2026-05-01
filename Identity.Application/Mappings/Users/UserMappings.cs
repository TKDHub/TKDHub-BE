using Identity.Application.Dtos.Tenants;
using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Branches;
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
                Username = model.Username.Trim(),
                Email = model.Email.Trim().ToLowerInvariant(),
                PhoneNumber = model.PhoneNumber?.Trim(),
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
                TenantId = user.TenantId,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = user.UserRoles.Select(r => r.RoleId.ToString()).ToList(),
                Status = (EntityStatusEnum)user.StatusId,
                EmailConfirmed = user.EmailConfirmed,
                FailedLoginAttempts = user.FailedLoginAttempts,
                LastLoginDate = user.LastLoginDate.HasValue ? new DateTimeOffset(user.LastLoginDate.Value, TimeSpan.Zero) : null,
                LockoutEnd = user.LockoutEnd.HasValue ? new DateTimeOffset(user.LockoutEnd.Value, TimeSpan.Zero) : null,
                CreatedOn = user.CreatedOn,
                CreatedByEmail = user.CreatedByEmail,
                CreatedByName = user.CreatedByName,
                ModifiedOn = user.ModifiedOn,
                ModifiedByEmail = user.ModifiedByEmail,
                ModifiedByName = user.ModifiedByName
            };
        }

        public static UserProfileDto ToProfileDto(this User user)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                TenantId = user.TenantId,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = user.UserRoles.Select(r => r.RoleId.ToString()).ToList(),
                Status = (EntityStatusEnum)user.StatusId,
                EmailConfirmed = user.EmailConfirmed,
                FailedLoginAttempts = user.FailedLoginAttempts,
                LastLoginDate = user.LastLoginDate.HasValue ? new DateTimeOffset(user.LastLoginDate.Value, TimeSpan.Zero) : null,
                LockoutEnd = user.LockoutEnd.HasValue ? new DateTimeOffset(user.LockoutEnd.Value, TimeSpan.Zero) : null,
                CreatedOn = user.CreatedOn,
                CreatedByEmail = user.CreatedByEmail,
                CreatedByName = user.CreatedByName,
                ModifiedOn = user.ModifiedOn,
                ModifiedByEmail = user.ModifiedByEmail,
                ModifiedByName = user.ModifiedByName,
                Branches = user.Branches?.ToListDtos() ?? new()
            };
        }

        public static List<UserProfileDto> ToListModels(this IEnumerable<User> users)
            => users.Select(u => u.ToProfileDto()).ToList();

        public static RegisterDto ToDto(this User user, TenantDto tenantDto)
        {
            return new RegisterDto
            {
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Tenant = tenantDto,
                Branches = user.Branches?.ToListDtos() ?? new(),
                Message = UserMessages.UserRegisteredSuccessfully,
            };
        }
    }
}
