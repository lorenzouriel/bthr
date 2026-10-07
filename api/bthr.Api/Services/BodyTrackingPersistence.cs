using Microsoft.EntityFrameworkCore;
using Npgsql;
using bthr.Api.Data;

namespace bthr.Api.Services;

public sealed class BodyTrackingConflictException(string message) : Exception(message);
public sealed class InvalidHabitException(string message) : Exception(message);

internal static class BodyTrackingPersistence
{
    public static async Task SaveChangesAsync(ApplicationDbContext context)
    {
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "uq_habits_user_name" or "uq_habit_logs_user_habit_date"
        })
        {
            // Also handle concurrent requests that pass the service's pre-check.
            throw new BodyTrackingConflictException("A habit name or daily habit log already exists, including archived/deleted records.");
        }
    }
}
