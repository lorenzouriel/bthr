using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/body/meals")]
[Authorize]
public class MealsController : ControllerBase
{
    private readonly IMealService _mealService;

    public MealsController(IMealService mealService)
    {
        _mealService = mealService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<MealResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMeals(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var meals = await _mealService.GetUserMealsAsync(userId, start_date, end_date);
        return Ok(meals);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MealResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateMeal(int userId, [FromBody] CreateMealRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var meal = await _mealService.CreateMealAsync(userId, request);
        return StatusCode(201, meal);
    }

    [HttpPut("{mealId}")]
    [ProducesResponseType(typeof(MealResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateMeal(int userId, int mealId, [FromBody] UpdateMealRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var meal = await _mealService.UpdateMealAsync(userId, mealId, request);
            if (meal == null)
                return NotFound(new { message = "Meal not found" });

            return Ok(meal);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{mealId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteMeal(int userId, int mealId)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var success = await _mealService.DeleteMealAsync(userId, mealId);
            if (!success)
                return NotFound(new { message = "Meal not found" });

            return Ok(new { message = "Meal deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
