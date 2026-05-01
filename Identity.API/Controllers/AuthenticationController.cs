using Identity.API.Filters;
using Identity.Application.Commands.Authentications;
using Identity.Application.Dtos.Users;
using Identity.Application.Models.Auth;
using Identity.Application.Models.User;
using Identity.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

        [RequireRestKey]
        [HttpPost("register")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(RegisterDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Register([FromBody] RegisterUserModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new RegisterCommand(model), cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error == UserErrors.EmailAlreadyExists || result.Error == UserErrors.UsernameAlreadyExists)
                    return Conflict(new { error = result.Error.Description });

                if (result.Error == TenantErrors.NotFound)
                    return NotFound(new { error = result.Error.Description });

                return BadRequest(new { error = result.Error.Description });
            }

            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        [RequireRestKey]
        [HttpPost("login")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(AuthDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status423Locked)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] AuthModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new LoginCommand(model), cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error == UserErrors.AccountLockedOut)
                    return StatusCode(StatusCodes.Status423Locked, new { error = result.Error.Description });

                return Unauthorized(new { error = result.Error.Description });
            }

            return Ok(result.Value);
        }

        [RequireRestKey]
        [HttpPost("refresh-token")]
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

        [RequireRestKey]
        [HttpPost("forgot-password")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new ForgotPasswordCommand(model), cancellationToken);

            if (result.IsFailure)
                return NotFound(new { error = result.Error.Description });

            return Ok(new { message = result.Value });
        }

        [RequireRestKey]
        [HttpPost("verify-otp")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new VerifyOtpCommand(model), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error.Description });

            return Ok(new { message = result.Value });
        }

        [RequireRestKey]
        [HttpPost("reset-password")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new ResetPasswordCommand(model), cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error == UserErrors.UserNotFound)
                    return NotFound(new { error = result.Error.Description });

                return BadRequest(new { error = result.Error.Description });
            }

            return Ok(new { message = result.Value });
        }

    }
}
