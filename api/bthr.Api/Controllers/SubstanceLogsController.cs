using bthr.Api.Data;
using Microsoft.EntityFrameworkCore;
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

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRecord(int userId, int id, [FromBody] CreateSubstanceLogRequest request, [FromServices] ApplicationDbContext db)
    {
        if (GetCurrentUserId() != userId) return Forbid();
        var record = await db.SubstanceLogs.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.Status != 0);
        if (record == null) return NotFound();
        record.ConsumedAt = request.ConsumedAt!.Value;
        record.SubstanceType = request.SubstanceType;
        record.Amount = request.Amount!.Value;
        record.Unit = request.Unit;
        record.Notes = request.Notes;
        await db.SaveChangesAsync();
        return Ok(new SubstanceLogResponse { Id = record.Id, UserId = record.UserId, ConsumedAt = record.ConsumedAt, SubstanceType = record.SubstanceType, Amount = record.Amount, Unit = record.Unit, Notes = record.Notes, Status = record.Status, CreatedAt = record.CreatedAt });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRecord(int userId, int id, [FromServices] ApplicationDbContext db)
    {
        if (GetCurrentUserId() != userId) return Forbid();
        var record = await db.SubstanceLogs.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.Status != 0);
        if (record == null) return NotFound();
        record.Status = 0;
        await db.SaveChangesAsync();
        return Ok(new { message = "Record deleted" });
    }
}
