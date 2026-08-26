using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/mind/meditation-sessions")]
[Authorize]
public class MeditationSessionsController : ControllerBase
{
    private readonly IMeditationSessionService _meditationSessionService;

    public MeditationSessionsController(IMeditationSessionService meditationSessionService)
    {
        _meditationSessionService = meditationSessionService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<MeditationSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMeditationSessions(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var sessions = await _meditationSessionService.GetUserMeditationSessionsAsync(userId, start_date, end_date);
        return Ok(sessions);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MeditationSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateMeditationSession(int userId, [FromBody] CreateMeditationSessionRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var session = await _meditationSessionService.CreateMeditationSessionAsync(userId, request);
        return StatusCode(201, session);
    }

    [HttpPut("{sessionId}")]
    [ProducesResponseType(typeof(MeditationSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateMeditationSession(int userId, int sessionId, [FromBody] UpdateMeditationSessionRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var session = await _meditationSessionService.UpdateMeditationSessionAsync(userId, sessionId, request);
            if (session == null)
                return NotFound(new { message = "Meditation session not found" });

            return Ok(session);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{sessionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteMeditationSession(int userId, int sessionId)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var success = await _meditationSessionService.DeleteMeditationSessionAsync(userId, sessionId);
            if (!success)
                return NotFound(new { message = "Meditation session not found" });

            return Ok(new { message = "Meditation session deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
