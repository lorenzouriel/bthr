using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

public class MeditationSessionBuilder
{
    private readonly MeditationSession _session;
    private static readonly Faker _faker = new Faker();

    public MeditationSessionBuilder()
    {
        _session = new MeditationSession
        {
            Id = 0,
            UserId = _faker.Random.Int(1, 100),
            SessionDate = _faker.Date.Recent(30),
            DurationMinutes = (short)_faker.Random.Int(5, 60),
            MeditationType = _faker.PickRandom(new[] { "Guided", "Breathing", "Body Scan", "Silent" }),
            MoodBefore = (short)_faker.Random.Int(1, 5),
            MoodAfter = (short)_faker.Random.Int(1, 5),
            CreatedAt = DateTime.UtcNow,
            Status = 1
        };
    }

    public MeditationSessionBuilder WithId(int id) { _session.Id = id; return this; }
    public MeditationSessionBuilder WithUserId(int userId) { _session.UserId = userId; return this; }
    public MeditationSessionBuilder WithSessionDate(DateTime date) { _session.SessionDate = date; return this; }
    public MeditationSessionBuilder WithMoodBefore(short? mood) { _session.MoodBefore = mood; return this; }
    public MeditationSessionBuilder WithMoodAfter(short? mood) { _session.MoodAfter = mood; return this; }
    public MeditationSessionBuilder AsActive() { _session.Status = 1; return this; }
    public MeditationSessionBuilder AsDeleted() { _session.Status = 0; return this; }
    public MeditationSession Build() => _session;
}
