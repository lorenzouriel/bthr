using System.Security.Claims;
using bthr.Api.Data;
using bthr.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace bthr.Api.Controllers;

[ApiController, Route("api/telegram"), Authorize, EnableRateLimiting("telegram")]
public class TelegramController(ApplicationDbContext db, TelegramLinks links, IConfiguration config) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public record ChallengeRequest(string Token);
    public record ClaimRequest(string Token, long Sender);

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var user = await db.Users.FindAsync(UserId);
        await links.Gate.WaitAsync();
        try { return Ok(new { userId = UserId, linked = user?.TelegramUserId != null, telegramUserId = user?.TelegramUserId?.ToString(), binding = links.Version(UserId) }); }
        finally { links.Gate.Release(); }
    }

    [HttpPost("challenge")]
    public async Task<IActionResult> Create()
    {
        if (User.HasClaim(x => x.Type == "telegram") || Request.Headers["X-Bthr-Link"] != "1") return Forbid();
        var name = config["Bot:Username"];
        if (string.IsNullOrWhiteSpace(name) || !System.Text.RegularExpressions.Regex.IsMatch(name, "^[A-Za-z0-9_]+$")) return StatusCode(503);
        var user = await db.Users.FindAsync(UserId);
        if (user?.Status != 1) return Forbid();
        await links.Gate.WaitAsync();
        try { var token = links.Create(UserId); return Ok(new { token, url = $"https://t.me/{name}?start={token}", expiresIn = 300 }); }
        finally { links.Gate.Release(); }
    }

    [AllowAnonymous, HttpPost("claim")]
    public async Task<IActionResult> Claim(ClaimRequest request)
    {
        if (!TelegramLinks.ServiceKey(Request, config)) return Unauthorized();
        if (request.Token == null || request.Token.Length > 64) return BadRequest();
        await links.Gate.WaitAsync();
        try { return links.Claim(request.Token, request.Sender) ? Ok() : BadRequest(new { message = "Link expired or unavailable" }); }
        finally { links.Gate.Release(); }
    }

    public record PhoneRequest(long Sender, string Phone);
    [AllowAnonymous, HttpPost("phone")]
    public async Task<IActionResult> Phone(PhoneRequest request)
    {
        if (!TelegramLinks.ServiceKey(Request, config)) return Unauthorized();
        if (request.Sender <= 0 || request.Phone == null || request.Phone.Length > 30) return BadRequest();
        var phone = TelegramLinks.NormalizePhone(request.Phone);
        if (phone.Length < 7 || phone.Length > 15) return BadRequest();
        await links.Gate.WaitAsync();
        try { return Ok(new { token = links.CreatePhone(request.Sender, phone) }); }
        finally { links.Gate.Release(); }
    }

    [HttpPost("phone-attach")]
    public async Task<IActionResult> PhoneAttach(ChallengeRequest request)
    {
        if (User.HasClaim(x => x.Type == "telegram") || Request.Headers["X-Bthr-Link"] != "1") return Forbid();
        if (request.Token == null || request.Token.Length > 64) return BadRequest();
        var user = await db.Users.FindAsync(UserId);
        if (user?.Status != 1) return Forbid();
        await links.Gate.WaitAsync();
        try { return links.AttachPhone(request.Token, UserId, user.PhoneNumber) ? Ok() : BadRequest(new { message = "Code unavailable or phone does not match your profile. Use QR linking instead." }); }
        finally { links.Gate.Release(); }
    }

    [HttpPost("pending")]
    public async Task<IActionResult> Pending(ChallengeRequest request)
    {
        if (request.Token == null || request.Token.Length > 64) return BadRequest();
        await links.Gate.WaitAsync();
        try { var pending = links.Get(request.Token); return pending?.UserId == UserId ? Ok(new { sender = pending.Sender?.ToString() }) : NotFound(); }
        finally { links.Gate.Release(); }
    }

    [HttpPost("approve")]
    public async Task<IActionResult> Approve(ChallengeRequest request)
    {
        if (User.HasClaim(x => x.Type == "telegram") || Request.Headers["X-Bthr-Link"] != "1") return Forbid();
        if (request.Token == null || request.Token.Length > 64) return BadRequest();
        await links.Gate.WaitAsync();
        try
        {
            var pending = links.Get(request.Token);
            if (pending?.UserId != UserId || pending.Sender == null) return BadRequest();
            var user = await db.Users.FindAsync(UserId);
            if (user?.Status != 1) return Forbid();
            if (await db.Users.AnyAsync(x => x.TelegramUserId == pending.Sender && x.Id != UserId)) return Conflict(new { message = "Telegram account is already linked" });
            user.TelegramUserId = pending.Sender;
            await db.SaveChangesAsync();
            links.Clear(UserId);
            return Ok();
        }
        finally { links.Gate.Release(); }
    }

    [HttpPost("unlink")]
    public async Task<IActionResult> Unlink()
    {
        if (!User.HasClaim(x => x.Type == "telegram") && Request.Headers["X-Bthr-Link"] != "1") return Forbid();
        await links.Gate.WaitAsync();
        try { var user = await db.Users.FindAsync(UserId); if (user == null) return NotFound(); user.TelegramUserId = null; await db.SaveChangesAsync(); links.Clear(UserId); return Ok(); }
        finally { links.Gate.Release(); }
    }
}
