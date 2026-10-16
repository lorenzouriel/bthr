using System.Security.Claims;
using bthr.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bthr.Api.Controllers;

[ApiController, Authorize, Route("api/meal-photos")]
public class MealPhotosController(ApplicationDbContext db, IConfiguration config) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Root => Path.GetFullPath(config["Photos:Root"] ?? Path.Combine(AppContext.BaseDirectory, "private-photos"));
    private string DirectoryFor(int mealId) => Path.Combine(Root, UserId.ToString(), mealId.ToString());
    private Task<bool> Owns(int mealId) => db.Meals.AnyAsync(x => x.Id == mealId && x.UserId == UserId && x.Status != 0);

    [HttpGet("{mealId:int}")]
    public async Task<IActionResult> List(int mealId)
    {
        if (!await Owns(mealId)) return NotFound();
        var directory = DirectoryFor(mealId);
        return Ok(Directory.Exists(directory) ? Directory.GetFiles(directory, "*.jpg").Select(Path.GetFileName).ToArray() : []);
    }

    [HttpPut("{mealId:int}/{photoId:guid}"), RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(int mealId, Guid photoId)
    {
        // Only the trusted agent can upload normalized JPEGs; browser uploads are not supported.
        if (!User.HasClaim(x => x.Type == "telegram")) return Forbid();
        if (!await Owns(mealId)) return NotFound();
        if (Request.ContentType != "image/jpeg") return BadRequest();
        using var buffer = new MemoryStream();
        var chunk = new byte[65536];
        int count;
        while ((count = await Request.Body.ReadAsync(chunk)) > 0)
        {
            if (buffer.Length + count > 10 * 1024 * 1024) return StatusCode(413);
            buffer.Write(chunk, 0, count);
        }
        var bytes = buffer.ToArray();
        if (bytes.Length < 4 || bytes[0] != 0xff || bytes[1] != 0xd8 || bytes[^2] != 0xff || bytes[^1] != 0xd9) return BadRequest();
        var directory = DirectoryFor(mealId);
        Directory.CreateDirectory(directory);
        var target = Path.Combine(directory, photoId.ToString("N") + ".jpg");
        if (!System.IO.File.Exists(target) && Directory.GetFiles(directory, "*.jpg").Length >= 10) return Conflict();
        var temporary = target + ".tmp-" + Guid.NewGuid().ToString("N");
        try { await System.IO.File.WriteAllBytesAsync(temporary, bytes); System.IO.File.Move(temporary, target, true); }
        finally { if (System.IO.File.Exists(temporary)) System.IO.File.Delete(temporary); }
        return Ok(new { photo = Path.GetFileName(target) });
    }

    [HttpGet("{mealId:int}/{photoId:guid}")]
    public async Task<IActionResult> Download(int mealId, Guid photoId)
    {
        if (!await Owns(mealId)) return NotFound();
        var path = Path.Combine(DirectoryFor(mealId), photoId.ToString("N") + ".jpg");
        if (!System.IO.File.Exists(path)) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        return PhysicalFile(path, "image/jpeg");
    }
}
