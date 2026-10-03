using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/body/habit-logs")]
[Authorize]
public class HabitLogsController : ControllerBase
{
    private readonly IHabitLogService _service;

    public HabitLogsController(IHabitLogService service)
    {
        _service = service;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) && id > 0 ? id : null;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<HabitLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHabitLogs(int userId,
        [FromQuery] DateOnly? start_date = null,
        [FromQuery] DateOnly? end_date = null,
        [FromQuery] int? habit_id = null)
    {
        if (GetCurrentUserId() != userId) return Forbid();
        if (start_date > end_date) return BadRequest(new { message = "start_date must not be after end_date." });
        return Ok(await _service.GetUserHabitLogsAsync(userId, start_date, end_date, habit_id));
    }

    [HttpPost]
    [ProducesResponseType(typeof(HabitLogResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateHabitLog(int userId, [FromBody] CreateHabitLogRequest request)
    {
        if (GetCurrentUserId() != userId) return Forbid();
        try
        {
            return StatusCode(StatusCodes.Status201Created, await _service.CreateHabitLogAsync(userId, request));
        }
        catch (BodyTrackingConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidHabitException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{habitLogId}")]
    [ProducesResponseType(typeof(HabitLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateHabitLog(int userId, int habitLogId, [FromBody] UpdateHabitLogRequest request)
    {
        if (GetCurrentUserId() != userId) return Forbid();
        try
        {
            var result = await _service.UpdateHabitLogAsync(userId, habitLogId, request);
            if (result == null) return NotFound(new { message = "HabitLog not found." });
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
        catch (InvalidHabitException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{habitLogId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteHabitLog(int userId, int habitLogId)
    {
        if (GetCurrentUserId() != userId) return Forbid();
        try
        {
            if (!await _service.DeleteHabitLogAsync(userId, habitLogId))
                return NotFound(new { message = "HabitLog not found." });
            return Ok(new { message = "HabitLog deleted successfully." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
