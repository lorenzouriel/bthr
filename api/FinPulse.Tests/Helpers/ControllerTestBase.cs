using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinPulse.Tests.Helpers;

/// <summary>
/// Base class for controller unit tests that provides helper methods for setting up authentication context.
/// </summary>
public abstract class ControllerTestBase
{
    /// <summary>
    /// Sets up the controller context with an authenticated user.
    /// This simulates a user being authenticated via JWT.
    /// </summary>
    /// <param name="controller">The controller to set up</param>
    /// <param name="userId">The user ID to authenticate with</param>
    protected void SetupControllerContext(ControllerBase controller, int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub", userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }
}
