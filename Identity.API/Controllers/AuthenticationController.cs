using Identity.Application.Commands.Authentications;
using Identity.Application.Dtos.Users;
using Identity.Application.Models.Auth;
using Identity.Application.Models.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
#pragma warning disable CS0618 // Keep legacy controller for backward compatibility

namespace Identity.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthenticationController : BaseApiController
    {
        private readonly ISender _sender;

        public AuthenticationController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("Register")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(RegisterDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Register([FromBody] RegisterUserModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new RegisterCommand(model), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error.Description });

            return CreatedAtAction(nameof(GetCurrentUser), result.Value);
        }

        [HttpPost("Login")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(AuthDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] AuthModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new LoginCommand(model), cancellationToken);

            if (result.IsFailure)
                return Unauthorized(new { error = result.Error.Description });

            return Ok(result.Value);
        }

        [HttpPost("Refresh-token")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(AuthDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new RefreshTokenCommand(model), cancellationToken);

            if (result.IsFailure)
                return Unauthorized(new { error = result.Error.Description });

            return Ok(result.Value);
        }

        [Authorize]
        [HttpPost("Change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model, CancellationToken cancellationToken)
        {
            var userId = GetUserIdFromClaims();
            if (userId == Guid.Empty)
                return Unauthorized();

            model.UserId = userId;

            var result = await _sender.Send(new ChangePasswordCommand(model), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error.Description });

            return Ok(new { message = "Password changed successfully" });
        }

        [Authorize]
        [HttpPost("Logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userId = GetUserIdFromClaims();
            if (userId == Guid.Empty)
                return Unauthorized(new { error = "Invalid token" });

            var result = await _sender.Send(new LogoutCommand(userId), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error.Description });

            return Ok(new { message = "Logged out successfully" });
        }

        [Authorize]
        [HttpGet("GetCurrentUser")]
        [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentUser()
        {
            var userId = GetUserIdFromClaims();
            var email = GetUserEmailFromClaims();
            var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value);

            return Ok(new CurrentUserDto(userId, email, roles));
        }
    }
}
