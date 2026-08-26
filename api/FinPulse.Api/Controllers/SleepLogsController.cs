using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/body/sleep-logs")]
[Authorize]
public class SleepLogsController : ControllerBase
{
    private readonly ISleepLogService _sleepLogService;

    public SleepLogsController(ISleepLogService sleepLogService)
    {
        _sleepLogService = sleepLogService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SleepLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSleepLogs(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var sleepLogs = await _sleepLogService.GetUserSleepLogsAsync(userId, start_date, end_date);
        return Ok(sleepLogs);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SleepLogResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSleepLog(int userId, [FromBody] CreateSleepLogRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var sleepLog = await _sleepLogService.CreateSleepLogAsync(userId, request);
        return StatusCode(201, sleepLog);
    }

    [HttpPut("{sleepLogId}")]
    [ProducesResponseType(typeof(SleepLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSleepLog(int userId, int sleepLogId, [FromBody] UpdateSleepLogRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var sleepLog = await _sleepLogService.UpdateSleepLogAsync(userId, sleepLogId, request);
            if (sleepLog == null)
                return NotFound(new { message = "Sleep log not found" });

            return Ok(sleepLog);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{sleepLogId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteSleepLog(int userId, int sleepLogId)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var success = await _sleepLogService.DeleteSleepLogAsync(userId, sleepLogId);
            if (!success)
                return NotFound(new { message = "Sleep log not found" });

            return Ok(new { message = "Sleep log deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
