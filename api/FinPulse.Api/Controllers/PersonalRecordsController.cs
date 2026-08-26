using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/body/personal-records")]
[Authorize]
public class PersonalRecordsController : ControllerBase
{
    private readonly IPersonalRecordService _personalRecordService;

    public PersonalRecordsController(IPersonalRecordService personalRecordService)
    {
        _personalRecordService = personalRecordService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<PersonalRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPersonalRecords(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var records = await _personalRecordService.GetUserPersonalRecordsAsync(userId, start_date, end_date);
        return Ok(records);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PersonalRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreatePersonalRecord(int userId, [FromBody] CreatePersonalRecordRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var record = await _personalRecordService.CreatePersonalRecordAsync(userId, request);
        return StatusCode(201, record);
    }
}
