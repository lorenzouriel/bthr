using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/body/habits")]
[Authorize]
public class HabitsController : ControllerBase
{
    private readonly IHabitService _service;

    public HabitsController(IHabitService service)
    {
        _service = service;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) && id > 0 ? id : null;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<HabitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetHabits(int userId)
    {
        if (GetCurrentUserId() != userId) return Forbid();
        return Ok(await _service.GetUserHabitsAsync(userId));
    }

    [HttpPost]
    [ProducesResponseType(typeof(HabitResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateHabit(int userId, [FromBody] CreateHabitRequest request)
    {
        if (GetCurrentUserId() != userId) return Forbid();
        try
        {
            return StatusCode(StatusCodes.Status201Created, await _service.CreateHabitAsync(userId, request));
        }
        catch (BodyTrackingConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{habitId}")]
    [ProducesResponseType(typeof(HabitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateHabit(int userId, int habitId, [FromBody] UpdateHabitRequest request)
    {
        if (GetCurrentUserId() != userId) return Forbid();
        try
        {
            var result = await _service.UpdateHabitAsync(userId, habitId, request);
            if (result == null) return NotFound(new { message = "Habit not found." });
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (BodyTrackingConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{habitId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteHabit(int userId, int habitId)
    {
        if (GetCurrentUserId() != userId) return Forbid();
        try
        {
            if (!await _service.DeleteHabitAsync(userId, habitId))
                return NotFound(new { message = "Habit not found." });
            return Ok(new { message = "Habit deleted successfully." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
