using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using bthr.Api.Data;
using bthr.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace bthr.Api.Controllers;

[ApiController, Authorize, Route("api/agent")]
public class AgentRecordsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("catalog")]
    public IActionResult Catalog() => Ok(AgentCatalog.Resources.Values.Select(r => new { r.Name, r.Path, create = AgentCatalog.Schema(r.Create), update = AgentCatalog.Schema(r.Update),
        numericFields = r.Entity.GetProperties().Where(p => new[] { typeof(decimal), typeof(double), typeof(int), typeof(short) }.Contains(Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType) && p.Name is not ("Id" or "UserId" or "Status") && !p.Name.EndsWith("Id")).Select(p => p.GetCustomAttribute<ColumnAttribute>()?.Name).Where(n => n != null),
        dateFields = r.Entity.GetProperties().Where(p => new[] { typeof(DateTime), typeof(DateOnly) }.Contains(Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType)).Select(p => p.GetCustomAttribute<ColumnAttribute>()?.Name).Where(n => n != null) }));

    [HttpGet("summary/{resource}")]
    public async Task<IActionResult> Summary(string resource, string field, string dateField, DateOnly start, DateOnly end)
    {
        if (!AgentCatalog.Resources.TryGetValue(resource, out var definition)) return NotFound();
        if (start == default || end < start || end.DayNumber - start.DayNumber > 366 || end.Year > 9998) return BadRequest();
        var props = definition.Entity.GetProperties();
        var numeric = props.FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Name == field &&
            new[] { typeof(decimal), typeof(double), typeof(int), typeof(short) }.Contains(Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType) && p.Name is not ("Id" or "UserId" or "Status") && !p.Name.EndsWith("Id"));
        var date = props.FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Name == dateField &&
            new[] { typeof(DateTime), typeof(DateOnly) }.Contains(Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType));
        if (numeric == null || date == null) return BadRequest();
        // Only identifier names obtained from compiled entity metadata reach SQL.
        var unit = props.Select(p => p.GetCustomAttribute<ColumnAttribute>()?.Name).FirstOrDefault(n => n is "currency_code" or "unit" or "dosage_unit");
        var unitSql = unit == null ? "''" : "\"" + unit + "\"";
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await db.Database.OpenConnectionAsync();
        await using var command = new NpgsqlCommand($"SELECT {unitSql}, count(*), count(\"{field}\"), sum(\"{field}\"), avg(\"{field}\"), min(\"{field}\"), max(\"{field}\") FROM {definition.SqlTable} WHERE user_id=@uid AND status<>0 AND \"{dateField}\">=@start AND \"{dateField}\"<@end GROUP BY 1", (NpgsqlConnection)db.Database.GetDbConnection());
        command.Parameters.AddWithValue("uid", userId);
        command.Parameters.AddWithValue("start", DateTime.SpecifyKind(start.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));
        command.Parameters.AddWithValue("end", DateTime.SpecifyKind(end.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));
        var results = new List<object>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) results.Add(new { unit = reader.IsDBNull(0) ? null : reader.GetString(0), records = reader.GetInt64(1), values = reader.GetInt64(2), sum = reader.IsDBNull(3) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(3)), average = reader.IsDBNull(4) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(4)), minimum = reader.IsDBNull(5) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(5)), maximum = reader.IsDBNull(6) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(6)) });
        return Ok(new { resource, field, dateField, start, end, groups = results, limitation = "Only recorded active values; dates use UTC for timestamp fields. Missing data is not zero. Units/currencies remain separate." });
    }

    [HttpGet("records/{resource}")]
    public async Task<IActionResult> Records(string resource, int? id = null, int offset = 0, int limit = 25)
    {
        if (!AgentCatalog.Resources.TryGetValue(resource, out var definition)) return NotFound();
        if (offset < 0 || limit < 1 || limit > 100 || id < 1) return BadRequest();
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await db.Database.OpenConnectionAsync();
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = new NpgsqlCommand($"SELECT row_to_json(t)::text, xmin::text FROM {definition.SqlTable} t WHERE user_id=@uid AND status <> 0 AND (@id IS NULL OR id=@id) ORDER BY id DESC LIMIT @limit OFFSET @offset", connection);
        command.Parameters.AddWithValue("uid", userId);
        command.Parameters.AddWithValue("id", NpgsqlTypes.NpgsqlDbType.Integer, (object?)id ?? DBNull.Value);
        command.Parameters.AddWithValue("limit", limit + 1);
        command.Parameters.AddWithValue("offset", offset);
        var rows = new List<object>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) rows.Add(new { record = JsonSerializer.Deserialize<JsonElement>(reader.GetString(0)), etag = "\"" + reader.GetString(1) + "\"" });
        return Ok(new { rows = rows.Take(limit), hasMore = rows.Count > limit, offset, limit });
    }
}
