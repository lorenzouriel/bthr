using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/bills")]
[Authorize]
public class BillsController : ControllerBase
{
    private readonly IBillService _billService;

    public BillsController(IBillService billService)
    {
        _billService = billService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BillResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBills(int userId, [FromQuery] int? year, [FromQuery] int? month)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var bills = await _billService.GetUserBillsAsync(userId, year, month);
        return Ok(bills);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BillResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateBill(int userId, [FromBody] CreateBillRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var bill = await _billService.CreateBillAsync(userId, request);
        return StatusCode(201, bill);
    }

    [HttpPut("{billId}")]
    [ProducesResponseType(typeof(BillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateBill(int userId, int billId, [FromBody] UpdateBillRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var bill = await _billService.UpdateBillAsync(userId, billId, request);
            if (bill == null)
                return NotFound(new { message = "Bill not found" });
            return Ok(bill);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{billId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteBill(int userId, int billId)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var success = await _billService.DeleteBillAsync(userId, billId);
            if (!success)
                return NotFound(new { message = "Bill not found" });
            return Ok(new { message = "Bill deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
