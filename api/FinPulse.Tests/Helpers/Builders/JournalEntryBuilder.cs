using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

public class JournalEntryBuilder
{
    private readonly JournalEntry _entry;
    private static readonly Faker _faker = new Faker();

    public JournalEntryBuilder()
    {
        _entry = new JournalEntry
        {
            Id = 0,
            UserId = _faker.Random.Int(1, 100),
            EntryDate = _faker.Date.Recent(30),
            Title = _faker.Lorem.Sentence(4),
            Content = _faker.Lorem.Paragraph(),
            Mood = (short)_faker.Random.Int(1, 5),
            Category = _faker.PickRandom(new[] { "Gratitude", "Reflection", "Goals" }),
            CreatedAt = DateTime.UtcNow,
            Status = 1
        };
    }

    public JournalEntryBuilder WithId(int id) { _entry.Id = id; return this; }
    public JournalEntryBuilder WithUserId(int userId) { _entry.UserId = userId; return this; }
    public JournalEntryBuilder WithEntryDate(DateTime date) { _entry.EntryDate = date; return this; }
    public JournalEntryBuilder WithMood(short? mood) { _entry.Mood = mood; return this; }
    public JournalEntryBuilder AsActive() { _entry.Status = 1; return this; }
    public JournalEntryBuilder AsDeleted() { _entry.Status = 0; return this; }
    public JournalEntry Build() => _entry;
}
