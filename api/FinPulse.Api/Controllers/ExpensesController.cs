using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/expenses")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ExpenseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetExpenses(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null,
        [FromQuery] string? category = null)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        var expenses = await _expenseService.GetUserExpensesAsync(userId, start_date, end_date, category);
        return Ok(expenses);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateExpense(int userId, [FromBody] CreateExpenseRequest request)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        var expense = await _expenseService.CreateExpenseAsync(userId, request);
        return StatusCode(201, expense);
    }

    [HttpPut("{expenseId}")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateExpense(int userId, int expenseId, [FromBody] UpdateExpenseRequest request)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        try
        {
            var expense = await _expenseService.UpdateExpenseAsync(userId, expenseId, request);

            if (expense == null)
            {
                return NotFound(new { message = "Expense not found" });
            }

            return Ok(expense);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{expenseId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteExpense(int userId, int expenseId)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        try
        {
            var success = await _expenseService.DeleteExpenseAsync(userId, expenseId);

            if (!success)
            {
                return NotFound(new { message = "Expense not found" });
            }

            return Ok(new { message = "Expense deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
