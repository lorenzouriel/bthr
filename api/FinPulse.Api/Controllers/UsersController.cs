using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using System.Security.Claims;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    private bool IsAdmin()
    {
        return User.FindFirst("admin")?.Value == "true";
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<UserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUser(int id)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(user);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var user = await _userService.UpdateUserAsync(id, request);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(DeleteUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var success = await _userService.DeleteUserAsync(id);

        if (!success)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(new DeleteUserResponse { Message = "User deleted successfully" });
    }
}
