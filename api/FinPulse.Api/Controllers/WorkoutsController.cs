using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/body/workouts")]
[Authorize]
public class WorkoutsController : ControllerBase
{
    private readonly IWorkoutService _workoutService;

    public WorkoutsController(IWorkoutService workoutService)
    {
        _workoutService = workoutService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<WorkoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWorkouts(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var workouts = await _workoutService.GetUserWorkoutsAsync(userId, start_date, end_date);
        return Ok(workouts);
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorkoutResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateWorkout(int userId, [FromBody] CreateWorkoutRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var workout = await _workoutService.CreateWorkoutAsync(userId, request);
        return StatusCode(201, workout);
    }

    [HttpPut("{workoutId}")]
    [ProducesResponseType(typeof(WorkoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateWorkout(int userId, int workoutId, [FromBody] UpdateWorkoutRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var workout = await _workoutService.UpdateWorkoutAsync(userId, workoutId, request);
            if (workout == null)
                return NotFound(new { message = "Workout not found" });

            return Ok(workout);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{workoutId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteWorkout(int userId, int workoutId)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var success = await _workoutService.DeleteWorkoutAsync(userId, workoutId);
            if (!success)
                return NotFound(new { message = "Workout not found" });

            return Ok(new { message = "Workout deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
