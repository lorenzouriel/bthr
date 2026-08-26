using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/body/body-metrics")]
[Authorize]
public class BodyMetricsController : ControllerBase
{
    private readonly IBodyMetricService _bodyMetricService;

    public BodyMetricsController(IBodyMetricService bodyMetricService)
    {
        _bodyMetricService = bodyMetricService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BodyMetricResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBodyMetrics(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var bodyMetrics = await _bodyMetricService.GetUserBodyMetricsAsync(userId, start_date, end_date);
        return Ok(bodyMetrics);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BodyMetricResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateBodyMetric(int userId, [FromBody] CreateBodyMetricRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var bodyMetric = await _bodyMetricService.CreateBodyMetricAsync(userId, request);
        return StatusCode(201, bodyMetric);
    }

    [HttpPut("{bodyMetricId}")]
    [ProducesResponseType(typeof(BodyMetricResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateBodyMetric(int userId, int bodyMetricId, [FromBody] UpdateBodyMetricRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var bodyMetric = await _bodyMetricService.UpdateBodyMetricAsync(userId, bodyMetricId, request);
            if (bodyMetric == null)
                return NotFound(new { message = "Body metric record not found" });

            return Ok(bodyMetric);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{bodyMetricId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteBodyMetric(int userId, int bodyMetricId)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var success = await _bodyMetricService.DeleteBodyMetricAsync(userId, bodyMetricId);
            if (!success)
                return NotFound(new { message = "Body metric record not found" });

            return Ok(new { message = "Body metric record deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
