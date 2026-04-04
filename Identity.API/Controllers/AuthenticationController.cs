using System.Security.Claims;
using Identity.Application.Commands.Authentications;
using Identity.Application.Dtos.Users;
using Identity.Application.Models.Auth;
using Identity.Application.Models.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthenticationController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("Register")]
        [ProducesResponseType(typeof(RegisterDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterUserModel model,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new RegisterCommand(model), cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error.Description });
            }

            return CreatedAtAction(nameof(GetCurrentUser), result.Value);
        }

        [HttpPost("Login")]
        [ProducesResponseType(typeof(AuthDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] AuthModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new LoginCommand(model), cancellationToken);

            if (result.IsFailure)
            {
                return Unauthorized(new { error = result.Error.Description });
            }

            return Ok(result.Value);
        }

        [HttpPost("Refresh-token")]
        [ProducesResponseType(typeof(AuthDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] string RefreshToken,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new RefreshTokenCommand(RefreshToken), cancellationToken);

            if (result.IsFailure)
            {
                return Unauthorized(new { error = result.Error.Description });
            }

            return Ok(result.Value);
        }

        [HttpPost("Change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model,
            CancellationToken cancellationToken)
        {
            var userId = GetUserIdFromClaims();

            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            model.UserId = userId;

            var result = await _sender.Send(new ChangePasswordCommand(model), cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error.Description });
            }

            return Ok(new { message = "Password changed successfully" });
        }

        [HttpPost("Logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            var result = await _sender.Send(new LogoutCommand(userId), cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error.Description });
            }

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("GetCurrentUser")]
        [Authorize]
        [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentUser()
        {
            var userId = GetUserIdFromClaims();
            var email = User.FindFirstValue(ClaimTypes.Email);
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);

            return Ok(new CurrentUserDto(userId, email!, roles));
        }

        private Guid GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}