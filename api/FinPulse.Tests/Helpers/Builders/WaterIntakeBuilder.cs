using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

public class WaterIntakeBuilder
{
    private readonly WaterIntake _waterIntake;
    private static readonly Faker _faker = new Faker();

    public WaterIntakeBuilder()
    {
        _waterIntake = new WaterIntake
        {
            Id = 0,
            UserId = _faker.Random.Int(1, 100),
            IntakeDate = _faker.Date.Recent(14),
            AmountMl = _faker.Random.Int(0, 3000),
            CreatedAt = DateTime.UtcNow,
            Status = 1
        };
    }

    public WaterIntakeBuilder WithId(int id) { _waterIntake.Id = id; return this; }
    public WaterIntakeBuilder WithUserId(int userId) { _waterIntake.UserId = userId; return this; }
    public WaterIntakeBuilder WithIntakeDate(DateTime date) { _waterIntake.IntakeDate = date; return this; }
    public WaterIntakeBuilder AsActive() { _waterIntake.Status = 1; return this; }
    public WaterIntakeBuilder AsDeleted() { _waterIntake.Status = 0; return this; }
    public WaterIntake Build() => _waterIntake;
}
