using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/body/weekly-routines")]
[Authorize]
public class WeeklyRoutinesController : ControllerBase
{
    private readonly IWeeklyRoutineService _weeklyRoutineService;

    public WeeklyRoutinesController(IWeeklyRoutineService weeklyRoutineService)
    {
        _weeklyRoutineService = weeklyRoutineService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<WeeklyRoutineResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWeeklyRoutines(int userId)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var routines = await _weeklyRoutineService.GetUserWeeklyRoutinesAsync(userId);
        return Ok(routines);
    }

    [HttpPost]
    [ProducesResponseType(typeof(WeeklyRoutineResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateWeeklyRoutine(int userId, [FromBody] CreateWeeklyRoutineRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var routine = await _weeklyRoutineService.CreateWeeklyRoutineAsync(userId, request);
        return StatusCode(201, routine);
    }

    [HttpPut("{routineId}")]
    [ProducesResponseType(typeof(WeeklyRoutineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateWeeklyRoutine(int userId, int routineId, [FromBody] UpdateWeeklyRoutineRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var routine = await _weeklyRoutineService.UpdateWeeklyRoutineAsync(userId, routineId, request);
            if (routine == null)
                return NotFound(new { message = "Weekly routine not found" });

            return Ok(routine);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{routineId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteWeeklyRoutine(int userId, int routineId)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var success = await _weeklyRoutineService.DeleteWeeklyRoutineAsync(userId, routineId);
            if (!success)
                return NotFound(new { message = "Weekly routine not found" });

            return Ok(new { message = "Weekly routine deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
