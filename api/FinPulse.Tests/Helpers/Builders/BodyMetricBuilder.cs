using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

public class BodyMetricBuilder
{
    private readonly BodyMetric _bodyMetric;
    private static readonly Faker _faker = new Faker();

    public BodyMetricBuilder()
    {
        _bodyMetric = new BodyMetric
        {
            Id = 0,
            UserId = _faker.Random.Int(1, 100),
            MeasuredDate = _faker.Date.Recent(90),
            WeightKg = _faker.Random.Decimal(50, 120),
            HeightCm = _faker.Random.Decimal(150, 200),
            BodyFatPercent = _faker.Random.Decimal(8, 35),
            CreatedAt = DateTime.UtcNow,
            Status = 1
        };
    }

    public BodyMetricBuilder WithId(int id) { _bodyMetric.Id = id; return this; }
    public BodyMetricBuilder WithUserId(int userId) { _bodyMetric.UserId = userId; return this; }
    public BodyMetricBuilder WithMeasuredDate(DateTime date) { _bodyMetric.MeasuredDate = date; return this; }
    public BodyMetricBuilder AsActive() { _bodyMetric.Status = 1; return this; }
    public BodyMetricBuilder AsDeleted() { _bodyMetric.Status = 0; return this; }
    public BodyMetric Build() => _bodyMetric;
}
