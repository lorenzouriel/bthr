using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using bthr.Api.DTOs;
using bthr.Api.Services;

namespace bthr.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/body/substance-logs")]
[Authorize]
public class SubstanceLogsController : ControllerBase
{
    private readonly ISubstanceLogService _service;

    public SubstanceLogsController(ISubstanceLogService service)
    {
        _service = service;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) && id > 0 ? id : null;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SubstanceLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSubstanceLogs(int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null)
    {
        if (GetCurrentUserId() != userId) return Forbid();
        if (start_date > end_date) return BadRequest(new { message = "start_date must not be after end_date." });
        return Ok(await _service.GetUserSubstanceLogsAsync(userId, start_date, end_date));
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubstanceLogResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSubstanceLog(int userId, [FromBody] CreateSubstanceLogRequest request)
    {
        if (GetCurrentUserId() != userId) return Forbid();
        return StatusCode(StatusCodes.Status201Created, await _service.CreateSubstanceLogAsync(userId, request));
    }
}
