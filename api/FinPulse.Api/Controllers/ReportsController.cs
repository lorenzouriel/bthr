using System.Security.Claims;
using FinPulse.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinPulse.Api.Controllers;

// Deterministic reporting only. No journal content, model access, or arbitrary user IDs.
[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController(ApplicationDbContext db) : ControllerBase
{
    public record Observation(int Id, DateTime Date, decimal? Value, string Unit = "");
    public record Metric(string Key, string Label, string Resource, string Unit, decimal? Value,
        decimal? PreviousValue, int Count, int PreviousCount, int[] RecordIds, string Calculation);

    public static Metric Summarize(string key, string label, string resource, string unit,
        IEnumerable<Observation> observations, DateTime start, DateTime endExclusive, bool average = false)
    {
        var rows = observations.ToArray();
        var previousStart = start - (endExclusive - start);
        var current = rows.Where(r => r.Date >= start && r.Date < endExclusive && r.Value.HasValue).ToArray();
        var previous = rows.Where(r => r.Date >= previousStart && r.Date < start && r.Value.HasValue).ToArray();
        decimal? Calculate(Observation[] values) => values.Length == 0 ? null :
            Math.Round(average ? values.Average(r => r.Value!.Value) : values.Sum(r => r.Value!.Value), 2);
        return new(key, label, resource, unit, Calculate(current), Calculate(previous), current.Length,
            previous.Length, current.Select(r => r.Id).ToArray(), average ? "Average of recorded values" : "Sum of recorded values");
    }

    [HttpGet("review")]
    public async Task<IActionResult> Review([FromQuery] DateOnly start_date, [FromQuery] DateOnly end_date,
        [FromQuery] string domain = "body", [FromQuery] bool combine = false)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        if (start_date == default || end_date == default || end_date < start_date || end_date.DayNumber - start_date.DayNumber > 366
            || start_date.Year < 1901 || end_date.Year > 9998)
            return BadRequest(new { message = "Choose a valid period of up to 367 days, starting after 1900." });
        if (!new[] { "finance", "body", "mind", "all" }.Contains(domain) || domain == "all" && !combine)
            return BadRequest(new { message = "Choose one domain, or explicitly enable a combined review." });
        var start = DateTime.SpecifyKind(start_date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(end_date.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var previousStart = start - (end - start);
        var metrics = new List<Metric>();
        void Add(string key, string label, string resource, string unit, IEnumerable<Observation> rows, bool average = false)
            => metrics.Add(Summarize(key, label, resource, unit, rows, start, end, average));

        if (domain is "finance" or "all")
        {
            var earnings = await db.Earnings.AsNoTracking().Where(r => r.UserId == userId && r.Status != 0 && r.EarningDate >= previousStart && r.EarningDate < end)
                .Select(r => new Observation(r.Id, r.EarningDate, r.Amount, r.CurrencyCode)).ToListAsync();
            var expenses = await db.Expenses.AsNoTracking().Where(r => r.UserId == userId && r.Status != 0 && r.ExpenseDate >= previousStart && r.ExpenseDate < end)
                .Select(r => new Observation(r.Id, r.ExpenseDate, r.Amount, r.CurrencyCode)).ToListAsync();
            foreach (var currency in earnings.Concat(expenses).Select(r => r.Unit).Distinct().Order())
            {
                Add("earnings-" + currency, "Recorded earnings", "earnings", currency, earnings.Where(r => r.Unit == currency));
                Add("expenses-" + currency, "Recorded expenses", "expenses", currency, expenses.Where(r => r.Unit == currency));
            }
        }
        if (domain is "body" or "all")
        {
            var water = await db.WaterIntakes.AsNoTracking().Where(r => r.UserId == userId && r.Status != 0 && r.IntakeDate >= previousStart && r.IntakeDate < end)
                .Select(r => new Observation(r.Id, r.IntakeDate, r.AmountMl, "")).ToListAsync();
            Add("water", "Recorded water", "water-intake", "ml", water);
            var workouts = await db.Workouts.AsNoTracking().Where(r => r.UserId == userId && r.Status != 0 && r.WorkoutDate >= previousStart && r.WorkoutDate < end)
                .Select(r => new Observation(r.Id, r.WorkoutDate, r.DurationMinutes, "")).ToListAsync();
            Add("workouts", "Workout time", "workouts", "min", workouts);
            var sleep = await db.SleepLogs.AsNoTracking().Where(r => r.UserId == userId && r.Status != 0 && r.BedTime >= previousStart && r.BedTime < end)
                .Select(r => new Observation(r.Id, r.BedTime, r.TotalHours, "")).ToListAsync();
            Add("sleep", "Average recorded sleep", "sleep-logs", "hours", sleep, true);
            var meals = await db.Meals.AsNoTracking().Where(r => r.UserId == userId && r.Status != 0 && r.MealDate >= previousStart && r.MealDate < end)
                .Select(r => new Observation(r.Id, r.MealDate, r.Calories, "")).ToListAsync();
            Add("meals", "Recorded meal energy", "meals", "kcal", meals);
        }
        if (domain is "mind" or "all")
        {
            var meditation = await db.MeditationSessions.AsNoTracking().Where(r => r.UserId == userId && r.Status != 0 && r.SessionDate >= previousStart && r.SessionDate < end)
                .Select(r => new { r.Id, r.SessionDate, r.DurationMinutes, r.MoodBefore, r.MoodAfter }).ToListAsync();
            Add("meditation", "Meditation time", "meditation-sessions", "min", meditation.Select(r => new Observation(r.Id, r.SessionDate, r.DurationMinutes)));
            Add("mood-before", "Meditation mood before", "meditation-sessions", "of 5", meditation.Select(r => new Observation(r.Id, r.SessionDate, r.MoodBefore)), true);
            Add("mood-after", "Meditation mood after", "meditation-sessions", "of 5", meditation.Select(r => new Observation(r.Id, r.SessionDate, r.MoodAfter)), true);
            var journal = await db.JournalEntries.AsNoTracking().Where(r => r.UserId == userId && r.Status != 0 && r.EntryDate >= previousStart && r.EntryDate < end)
                .Select(r => new Observation(r.Id, r.EntryDate, r.Mood, "")).ToListAsync();
            Add("journal-mood", "Journal mood", "journal-entries", "of 5", journal, true);
        }
        return Ok(new { startDate = start_date, endDate = end_date, previousStartDate = DateOnly.FromDateTime(previousStart),
            previousEndDate = start_date.AddDays(-1), domain, metrics,
            limitation = "Missing records are not zero. Comparisons describe recorded data, not complete activity. Sleep is grouped by bedtime in UTC; date-only activities retain their recorded date." });
    }
}
