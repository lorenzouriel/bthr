using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Filters;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/budgets")]
[Authorize]
[RequiresPlan(1)]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;

    public BudgetsController(IBudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BudgetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBudgets(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        var budgets = await _budgetService.GetUserBudgetsAsync(userId, start_date, end_date);
        return Ok(budgets);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BudgetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateBudget(int userId, [FromBody] CreateBudgetRequest request)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        var budget = await _budgetService.CreateBudgetAsync(userId, request);
        return StatusCode(201, budget);
    }

    [HttpPut("{budgetId}")]
    [ProducesResponseType(typeof(BudgetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateBudget(int userId, int budgetId, [FromBody] UpdateBudgetRequest request)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        try
        {
            var budget = await _budgetService.UpdateBudgetAsync(userId, budgetId, request);

            if (budget == null)
            {
                return NotFound(new { message = "Budget not found" });
            }

            return Ok(budget);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{budgetId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteBudget(int userId, int budgetId)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        try
        {
            var success = await _budgetService.DeleteBudgetAsync(userId, budgetId);

            if (!success)
            {
                return NotFound(new { message = "Budget not found" });
            }

            return Ok(new { message = "Budget deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
