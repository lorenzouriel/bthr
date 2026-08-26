using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

public class PersonalRecordBuilder
{
    private readonly PersonalRecord _record;
    private static readonly Faker _faker = new Faker();

    public PersonalRecordBuilder()
    {
        _record = new PersonalRecord
        {
            Id = 0,
            UserId = _faker.Random.Int(1, 100),
            ExerciseName = _faker.PickRandom(new[] { "Bench Press", "Deadlift", "Squat", "5k Run" }),
            MetricType = _faker.PickRandom(new[] { "Max Weight", "Max Reps", "Best Time" }),
            Value = _faker.Random.Decimal(10, 200),
            Unit = _faker.PickRandom(new[] { "kg", "reps", "seconds" }),
            AchievedDate = _faker.Date.Recent(90),
            CreatedAt = DateTime.UtcNow,
            Status = 1
        };
    }

    public PersonalRecordBuilder WithId(int id) { _record.Id = id; return this; }
    public PersonalRecordBuilder WithUserId(int userId) { _record.UserId = userId; return this; }
    public PersonalRecordBuilder WithAchievedDate(DateTime date) { _record.AchievedDate = date; return this; }
    public PersonalRecordBuilder AsActive() { _record.Status = 1; return this; }
    public PersonalRecordBuilder AsDeleted() { _record.Status = 0; return this; }
    public PersonalRecord Build() => _record;
}
