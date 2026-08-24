using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/earnings")]
[Authorize]
public class EarningsController : ControllerBase
{
    private readonly IEarningService _earningService;

    public EarningsController(IEarningService earningService)
    {
        _earningService = earningService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<EarningResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEarnings(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null,
        [FromQuery] string? category = null)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        var earnings = await _earningService.GetUserEarningsAsync(userId, start_date, end_date, category);
        return Ok(earnings);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EarningResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEarning(int userId, [FromBody] CreateEarningRequest request)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        var earning = await _earningService.CreateEarningAsync(userId, request);
        return StatusCode(201, earning);
    }

    [HttpPut("{earningId}")]
    [ProducesResponseType(typeof(EarningResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateEarning(int userId, int earningId, [FromBody] UpdateEarningRequest request)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        try
        {
            var earning = await _earningService.UpdateEarningAsync(userId, earningId, request);

            if (earning == null)
            {
                return NotFound(new { message = "Earning not found" });
            }

            return Ok(earning);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{earningId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteEarning(int userId, int earningId)
    {
        if (GetCurrentUserId() != userId)
        {
            return Forbid();
        }

        try
        {
            var success = await _earningService.DeleteEarningAsync(userId, earningId);

            if (!success)
            {
                return NotFound(new { message = "Earning not found" });
            }

            return Ok(new { message = "Earning deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
