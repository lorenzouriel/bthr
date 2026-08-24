using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Filters;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/investments")]
[Authorize]
[RequiresPlan(1)]
public class InvestmentsController : ControllerBase
{
    private readonly IInvestmentService _investmentService;

    public InvestmentsController(IInvestmentService investmentService)
    {
        _investmentService = investmentService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<InvestmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInvestments(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null,
        [FromQuery] string? investment_type = null,
        [FromQuery] string? category = null)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        var investments = await _investmentService.GetUserInvestmentsAsync(userId, start_date, end_date, investment_type, category);
        return Ok(investments);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InvestmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateInvestment(int userId, [FromBody] CreateInvestmentRequest request)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        var investment = await _investmentService.CreateInvestmentAsync(userId, request);
        return StatusCode(201, investment);
    }

    [HttpPut("{investmentId}")]
    [ProducesResponseType(typeof(InvestmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateInvestment(int userId, int investmentId, [FromBody] UpdateInvestmentRequest request)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        try
        {
            var investment = await _investmentService.UpdateInvestmentAsync(userId, investmentId, request);

            if (investment == null)
            {
                return NotFound(new { message = "Investment not found" });
            }

            return Ok(investment);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{investmentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteInvestment(int userId, int investmentId)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        try
        {
            var success = await _investmentService.DeleteInvestmentAsync(userId, investmentId);

            if (!success)
            {
                return NotFound(new { message = "Investment not found" });
            }

            return Ok(new { message = "Investment deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
