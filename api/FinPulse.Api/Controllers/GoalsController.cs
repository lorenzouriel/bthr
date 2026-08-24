using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Filters;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/goals")]
[Authorize]
[RequiresPlan(1)]
public class GoalsController : ControllerBase
{
    private readonly IGoalService _goalService;

    public GoalsController(IGoalService goalService)
    {
        _goalService = goalService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<GoalResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGoals(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        var goals = await _goalService.GetUserGoalsAsync(userId, start_date, end_date);
        return Ok(goals);
    }

    [HttpPost]
    [ProducesResponseType(typeof(GoalResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateGoal(int userId, [FromBody] CreateGoalRequest request)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        var goal = await _goalService.CreateGoalAsync(userId, request);
        return StatusCode(201, goal);
    }

    [HttpPut("{goalId}")]
    [ProducesResponseType(typeof(GoalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateGoal(int userId, int goalId, [FromBody] UpdateGoalRequest request)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        try
        {
            var goal = await _goalService.UpdateGoalAsync(userId, goalId, request);

            if (goal == null)
            {
                return NotFound(new { message = "Goal not found" });
            }

            return Ok(goal);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{goalId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteGoal(int userId, int goalId)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        try
        {
            var success = await _goalService.DeleteGoalAsync(userId, goalId);

            if (!success)
            {
                return NotFound(new { message = "Goal not found" });
            }

            return Ok(new { message = "Goal deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
