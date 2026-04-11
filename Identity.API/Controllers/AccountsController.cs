using Identity.Application.Commands.Authentications;
using Identity.Application.Commands.Users;
using Identity.Application.Dtos.Users;
using Identity.Application.Models.Auth;
using Identity.Application.Models.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Identity.API.Controllers
{
    [Route("api/v1/accounts")]
    public class AccountsController : BaseApiController
    {
        private readonly ISender _sender;

        public AccountsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Login with username (email) and password</summary>
        [HttpPost("login")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(AuthDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status423Locked)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] AuthModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new LoginCommand(model), cancellationToken);

            if (result.IsFailure)
                return Unauthorized(new { error = result.Error.Description });

            return Ok(result.Value);
        }

        /// <summary>Register a new account</summary>
        [HttpPost("register")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(RegisterDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Register([FromBody] RegisterUserModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new RegisterCommand(model), cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error.Code == "User.EmailAlreadyExists")
                    return Conflict(new { error = result.Error.Description });

                if (result.Error.Code == "Tenant.NotFound")
                    return NotFound(new { error = result.Error.Description });

                return BadRequest(new { error = result.Error.Description });
            }

            return Ok(result.Value);
        }

        /// <summary>Refresh access token using a valid refresh token</summary>
        [HttpPost("refreshtoken")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(AuthDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new RefreshTokenCommand(model), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error.Description });

            return Ok(result.Value);
        }

        /// <summary>Change password for the authenticated user</summary>
        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model, CancellationToken cancellationToken)
        {
            var userId = GetUserIdFromClaims();
            if (userId == Guid.Empty)
                return Unauthorized();

            model.UserId = userId;

            var result = await _sender.Send(new ChangePasswordCommand(model), cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error.Code == "User.NotFound")
                    return NotFound(new { error = result.Error.Description });

                return BadRequest(new { error = result.Error.Description });
            }

            return Ok(new { message = "Password changed successfully" });
        }

        /// <summary>Reset password using a reset key (issued via forgot-password flow)</summary>
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new ResetPasswordCommand(model), cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error.Code == "User.NotFound")
                    return NotFound(new { error = result.Error.Description });

                if (result.Error.Code.Contains("Forbidden"))
                    return Forbid();

                return BadRequest(new { error = result.Error.Description });
            }

            return Ok(new { message = "Password reset successfully" });
        }

        /// <summary>Update account details (email, status, actor type, phone number)</summary>
        [Authorize]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] UpdateAccountModel model, CancellationToken cancellationToken)
        {
            var requestedByUserId = GetUserIdFromClaims();
            if (requestedByUserId == Guid.Empty)
                return Unauthorized();

            model.UserId = id;
            model.ModifiedByEmail = GetUserEmailFromClaims();
            model.ModifiedByName = GetUserNameFromClaims();

            var result = await _sender.Send(new UpdateAccountCommand(id, model), cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error.Code == "User.NotFound")
                    return NotFound(new { error = result.Error.Description });

                if (result.Error.Code.Contains("Forbidden"))
                    return Forbid();

                return BadRequest(new { error = result.Error.Description });
            }

            return NoContent();
        }

        /// <summary>Delete an account</summary>
        [Authorize]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAccount(Guid id, CancellationToken cancellationToken)
        {
            var requestedByUserId = GetUserIdFromClaims();
            if (requestedByUserId == Guid.Empty)
                return Unauthorized();

            var result = await _sender.Send(new DeleteUserCommand(id, requestedByUserId), cancellationToken);

            if (result.IsFailure)
                return result.Error.Code.Contains("Forbidden")
                    ? Forbid()
                    : NotFound(new { error = result.Error.Description });

            return NoContent();
        }
    }
}
