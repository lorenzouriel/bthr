using System.Security.Claims;
using bthr.Api.Data;
using bthr.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace bthr.Api.Filters;

// Locks and compares xmin in the same transaction as the existing service mutation.
public sealed class AgentConcurrencyFilter(ApplicationDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!context.HttpContext.User.HasClaim(x => x.Type == "telegram") || request.Method is not ("PUT" or "DELETE") || !request.Path.StartsWithSegments("/api/users")) { await next(); return; }
        var segments = request.Path.Value!.Split('/');
        if (segments.Length < 6 || !AgentCatalog.Resources.TryGetValue(segments[^2], out var resource) || !int.TryParse(segments[^1], out var id)) { context.Result = new BadRequestResult(); return; }
        if (!request.Headers.TryGetValue("If-Match", out var expected)) { context.Result = new StatusCodeResult(428); return; }
        var userId = int.Parse(context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = new NpgsqlCommand($"SELECT xmin::text FROM {resource.SqlTable} WHERE id=@id AND user_id=@uid AND status<>0 FOR UPDATE", connection, (NpgsqlTransaction)transaction.GetDbTransaction());
        command.Parameters.AddWithValue("id", id); command.Parameters.AddWithValue("uid", userId);
        var version = await command.ExecuteScalarAsync();
        if (version == null) { context.Result = new NotFoundResult(); return; }
        if (expected != "\"" + version + "\"") { context.Result = new StatusCodeResult(412); return; }
        var result = await next();
        if (result.Exception == null && (result.Result as ObjectResult)?.StatusCode is not >= 400) await transaction.CommitAsync();
    }
}
