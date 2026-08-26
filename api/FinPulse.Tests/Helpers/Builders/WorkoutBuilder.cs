using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

public class WorkoutBuilder
{
    private readonly Workout _workout;
    private static readonly Faker _faker = new Faker();

    public WorkoutBuilder()
    {
        _workout = new Workout
        {
            Id = 0,
            UserId = _faker.Random.Int(1, 100),
            WorkoutDate = _faker.Date.Recent(30),
            RoutineName = _faker.PickRandom(new[] { "Push Day", "Pull Day", "Leg Day", "Rest Day" }),
            DurationMinutes = _faker.Random.Int(20, 90),
            CaloriesBurned = _faker.Random.Decimal(150, 600),
            CreatedAt = DateTime.UtcNow,
            Status = 1
        };
    }

    public WorkoutBuilder WithId(int id) { _workout.Id = id; return this; }
    public WorkoutBuilder WithUserId(int userId) { _workout.UserId = userId; return this; }
    public WorkoutBuilder WithWorkoutDate(DateTime date) { _workout.WorkoutDate = date; return this; }
    public WorkoutBuilder WithRoutineName(string name) { _workout.RoutineName = name; return this; }
    public WorkoutBuilder AsActive() { _workout.Status = 1; return this; }
    public WorkoutBuilder AsDeleted() { _workout.Status = 0; return this; }
    public Workout Build() => _workout;
}
