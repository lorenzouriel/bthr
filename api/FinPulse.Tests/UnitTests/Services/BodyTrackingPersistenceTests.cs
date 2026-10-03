using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;
using FinPulse.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinPulse.Tests.UnitTests.Services;

public class BodyTrackingPersistenceTests
{
    [Theory]
    [InlineData("uq_habits_user_name")]
    [InlineData("uq_habit_logs_user_habit_date")]
    public async Task ConcurrentUniqueViolation_IsTranslatedToConflict(string constraint)
    {
        using var db = new FailingSaveContext(constraint);
        var service = new HabitService(db);
        var create = () => service.CreateHabitAsync(1, new CreateHabitRequest { HabitName = "Read" });
        await create.Should().ThrowAsync<BodyTrackingConflictException>();
    }

    [Fact]
    public async Task UnrelatedDatabaseError_IsNotMisreportedAsConflict()
    {
        using var db = new FailingSaveContext("unrelated_constraint");
        var service = new HabitService(db);
        var create = () => service.CreateHabitAsync(1, new CreateHabitRequest { HabitName = "Read" });
        await create.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public void PostgreSqlMappings_MatchExistingBodyTables()
    {
        // Building the provider model does not open a database connection.
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_test;Username=model_test").Options);
        foreach (var (type, table) in new[]
        {
            (typeof(Habit), "habits"), (typeof(HabitLog), "habit_logs"),
            (typeof(SubstanceLog), "substance_logs"), (typeof(SymptomLog), "symptom_logs")
        })
        {
            var entity = db.Model.FindEntityType(type)!;
            entity.GetSchema().Should().Be("body");
            entity.GetTableName().Should().Be(table);
            entity.FindProperty("Status")!.GetColumnType().Should().Be("smallint");
            entity.FindProperty("CreatedAt")!.GetColumnType().Should().Be("timestamp with time zone");
            entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(User))
                .DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        }
        var habit = db.Model.FindEntityType(typeof(Habit))!;
        habit.GetIndexes().Single(index => index.GetDatabaseName() == "uq_habits_user_name").IsUnique.Should().BeTrue();
        var log = db.Model.FindEntityType(typeof(HabitLog))!;
        log.GetIndexes().Single(index => index.GetDatabaseName() == "uq_habit_logs_user_habit_date").IsUnique.Should().BeTrue();
        log.FindProperty("LogDate")!.GetColumnType().Should().Be("date");
        log.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Habit))
            .DeleteBehavior.Should().Be(DeleteBehavior.NoAction);
        db.Model.FindEntityType(typeof(SymptomLog))!.FindProperty("LogDate")!.GetColumnType().Should().Be("date");
        db.Model.FindEntityType(typeof(SubstanceLog))!.FindProperty("ConsumedAt")!.GetColumnType().Should().Be("timestamp with time zone");
        db.Model.FindEntityType(typeof(SubstanceLog))!.FindProperty("Amount")!.GetColumnType().Should().Be("numeric(6,2)");
    }

    private sealed class FailingSaveContext(string constraint)
        : ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("Simulated concurrent insert",
                new PostgresException("duplicate", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation,
                    constraintName: constraint));
    }
}
