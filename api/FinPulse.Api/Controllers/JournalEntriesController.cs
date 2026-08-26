using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/mind/journal-entries")]
[Authorize]
public class JournalEntriesController : ControllerBase
{
    private readonly IJournalEntryService _journalEntryService;

    public JournalEntriesController(IJournalEntryService journalEntryService)
    {
        _journalEntryService = journalEntryService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<JournalEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetJournalEntries(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var entries = await _journalEntryService.GetUserJournalEntriesAsync(userId, start_date, end_date);
        return Ok(entries);
    }

    [HttpPost]
    [ProducesResponseType(typeof(JournalEntryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateJournalEntry(int userId, [FromBody] CreateJournalEntryRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var entry = await _journalEntryService.CreateJournalEntryAsync(userId, request);
        return StatusCode(201, entry);
    }

    [HttpPut("{entryId}")]
    [ProducesResponseType(typeof(JournalEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateJournalEntry(int userId, int entryId, [FromBody] UpdateJournalEntryRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var entry = await _journalEntryService.UpdateJournalEntryAsync(userId, entryId, request);
            if (entry == null)
                return NotFound(new { message = "Journal entry not found" });

            return Ok(entry);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{entryId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteJournalEntry(int userId, int entryId)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var success = await _journalEntryService.DeleteJournalEntryAsync(userId, entryId);
            if (!success)
                return NotFound(new { message = "Journal entry not found" });

            return Ok(new { message = "Journal entry deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
