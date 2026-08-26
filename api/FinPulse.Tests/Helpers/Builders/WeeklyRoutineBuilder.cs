using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

public class WeeklyRoutineBuilder
{
    private readonly WeeklyRoutine _routine;
    private static readonly Faker _faker = new Faker();

    public WeeklyRoutineBuilder()
    {
        _routine = new WeeklyRoutine
        {
            Id = 0,
            UserId = _faker.Random.Int(1, 100),
            DayOfWeek = (short)_faker.Random.Int(0, 6),
            RoutineName = _faker.PickRandom(new[] { "Push Day", "Pull Day", "Leg Day", "Rest Day", "Cardio" }),
            Description = _faker.Lorem.Sentence(),
            CreatedAt = DateTime.UtcNow,
            Status = 1
        };
    }

    public WeeklyRoutineBuilder WithId(int id) { _routine.Id = id; return this; }
    public WeeklyRoutineBuilder WithUserId(int userId) { _routine.UserId = userId; return this; }
    public WeeklyRoutineBuilder WithDayOfWeek(short dayOfWeek) { _routine.DayOfWeek = dayOfWeek; return this; }
    public WeeklyRoutineBuilder WithRoutineName(string name) { _routine.RoutineName = name; return this; }
    public WeeklyRoutineBuilder AsActive() { _routine.Status = 1; return this; }
    public WeeklyRoutineBuilder AsDeleted() { _routine.Status = 0; return this; }
    public WeeklyRoutine Build() => _routine;
}
