using Identity.Application.Commands.Authentications;
using Identity.Application.Commands.Users;
using Identity.Application.Dtos.Users;
using Identity.Application.Models.Auth;
using Identity.Application.Models.User;
using Identity.Application.Queries.Users;
using Identity.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Pagination;

namespace Identity.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class UserController : BaseApiController
    {
        private readonly ISender _sender;

        public UserController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("getCurrentUser")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
        {
            var userId = GetUserIdFromClaims();
            if (userId == Guid.Empty)
                return Unauthorized();

            var result = await _sender.Send(new GetCurrentUserQuery(userId), cancellationToken);

            if (result.IsFailure)
                return NotFound(new { error = result.Error.Description });

            return Ok(result.Value);
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<UserProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllUsers([FromQuery] PagedRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetAllUsersQuery(request), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error.Description });

            return Ok(result.Value);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetUserByIdQuery(id), cancellationToken);

            if (result.IsFailure)
                return NotFound(new { error = result.Error.Description });

            return Ok(result.Value);
        }

        [HttpPut("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserModel model, CancellationToken cancellationToken)
        {
            var userId = GetUserIdFromClaims();
            if (userId == Guid.Empty)
                return Unauthorized();

            model.UserId = userId;
            model.ModifiedByEmail = GetUserEmailFromClaims();
            model.ModifiedByName = GetUserNameFromClaims();

            var result = await _sender.Send(new UpdateProfileCommand(model), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error.Description });

            return Ok(result.Value);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
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
                if (result.Error == UserErrors.UserNotFound)
                    return NotFound(new { error = result.Error.Description });

                if (result.Error == UserErrors.Forbidden)
                    return Forbid();

                return BadRequest(new { error = result.Error.Description });
            }

            return Ok(result.Value);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            var requestedByUserId = GetUserIdFromClaims();
            if (requestedByUserId == Guid.Empty)
                return Unauthorized();

            var result = await _sender.Send(new DeleteUserCommand(id, requestedByUserId), cancellationToken);

            if (result.IsFailure)
                return result.Error == UserErrors.Forbidden
                    ? Forbid()
                    : NotFound(new { error = result.Error.Description });

            return NoContent();
        }

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
                if (result.Error == UserErrors.UserNotFound)
                    return NotFound(new { error = result.Error.Description });

                return BadRequest(new { error = result.Error.Description });
            }

            return Ok(new { message = UserMessages.PasswordChangedSuccessfully });
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userId = GetUserIdFromClaims();
            if (userId == Guid.Empty)
                return Unauthorized(new { error = UserErrors.InvalidToken.Description });

            var result = await _sender.Send(new LogoutCommand(userId), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error.Description });

            return Ok(new { message = UserMessages.LoggedOutSuccessfully });
        }
    }
}
