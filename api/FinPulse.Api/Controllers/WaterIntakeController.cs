using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/body/water-intake")]
[Authorize]
public class WaterIntakeController : ControllerBase
{
    private readonly IWaterIntakeService _waterIntakeService;

    public WaterIntakeController(IWaterIntakeService waterIntakeService)
    {
        _waterIntakeService = waterIntakeService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<WaterIntakeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWaterIntake(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var waterIntake = await _waterIntakeService.GetUserWaterIntakeAsync(userId, start_date, end_date);
        return Ok(waterIntake);
    }

    [HttpPost]
    [ProducesResponseType(typeof(WaterIntakeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateWaterIntake(int userId, [FromBody] CreateWaterIntakeRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var waterIntake = await _waterIntakeService.CreateWaterIntakeAsync(userId, request);
        return StatusCode(201, waterIntake);
    }

    [HttpPut("{waterIntakeId}")]
    [ProducesResponseType(typeof(WaterIntakeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateWaterIntake(int userId, int waterIntakeId, [FromBody] UpdateWaterIntakeRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var waterIntake = await _waterIntakeService.UpdateWaterIntakeAsync(userId, waterIntakeId, request);
            if (waterIntake == null)
                return NotFound(new { message = "Water intake record not found" });

            return Ok(waterIntake);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{waterIntakeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteWaterIntake(int userId, int waterIntakeId)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var success = await _waterIntakeService.DeleteWaterIntakeAsync(userId, waterIntakeId);
            if (!success)
                return NotFound(new { message = "Water intake record not found" });

            return Ok(new { message = "Water intake record deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
