using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

public class SleepLogBuilder
{
    private readonly SleepLog _sleepLog;
    private static readonly Faker _faker = new Faker();

    public SleepLogBuilder()
    {
        var bedTime = _faker.Date.Recent(14);

        _sleepLog = new SleepLog
        {
            Id = 0,
            UserId = _faker.Random.Int(1, 100),
            BedTime = bedTime,
            WakeTime = bedTime.AddHours(_faker.Random.Double(5, 9)),
            Notes = _faker.Lorem.Sentence(),
            CreatedAt = DateTime.UtcNow,
            Status = 1
        };
    }

    public SleepLogBuilder WithId(int id) { _sleepLog.Id = id; return this; }
    public SleepLogBuilder WithUserId(int userId) { _sleepLog.UserId = userId; return this; }
    public SleepLogBuilder WithBedTime(DateTime bedTime) { _sleepLog.BedTime = bedTime; return this; }
    public SleepLogBuilder WithWakeTime(DateTime wakeTime) { _sleepLog.WakeTime = wakeTime; return this; }
    public SleepLogBuilder AsActive() { _sleepLog.Status = 1; return this; }
    public SleepLogBuilder AsDeleted() { _sleepLog.Status = 0; return this; }
    public SleepLog Build() => _sleepLog;
}
