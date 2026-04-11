using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers
{
    /// <summary>
    /// Base controller providing common helper methods shared across all API controllers.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Extracts the authenticated user's ID from JWT claims.
        /// Returns Guid.Empty if the claim is missing or invalid.
        /// </summary>
        protected Guid GetUserIdFromClaims()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("userId");

            return Guid.TryParse(claim, out var userId) ? userId : Guid.Empty;
        }

        /// <summary>
        /// Extracts the authenticated user's email from JWT claims.
        /// </summary>
        protected string GetUserEmailFromClaims()
            => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        /// <summary>
        /// Extracts the authenticated user's full name from JWT claims.
        /// </summary>
        protected string GetUserNameFromClaims()
            => User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    }
}
