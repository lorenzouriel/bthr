using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class JournalEntryServiceTests : ServiceTestBase
{
    private readonly JournalEntryService _sut;

    public JournalEntryServiceTests()
    {
        _sut = new JournalEntryService(Context);
    }

    #region CreateJournalEntryAsync Tests

    [Fact]
    public async Task CreateJournalEntryAsync_WithValidRequest_CreatesSuccessfully()
    {
        var userId = 1;
        var request = new CreateJournalEntryRequest
        {
            EntryDate = DateTime.UtcNow.Date,
            Title = "A good day",
            Content = "Today was a productive day.",
            Mood = 4,
            Category = "Reflection"
        };

        var result = await _sut.CreateJournalEntryAsync(userId, request);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.Title.Should().Be("A good day");
        result.Mood.Should().Be(4);

        var entry = await Context.JournalEntries.FindAsync(result.Id);
        entry!.Status.Should().Be(1);
    }

    [Fact]
    public async Task CreateJournalEntryAsync_WithNullMood_Succeeds()
    {
        var userId = 1;
        var request = new CreateJournalEntryRequest
        {
            EntryDate = DateTime.UtcNow.Date,
            Content = "Just writing without a mood rating today."
        };

        var result = await _sut.CreateJournalEntryAsync(userId, request);

        result.Should().NotBeNull();
        result.Mood.Should().BeNull();
    }

    #endregion

    #region GetUserJournalEntriesAsync Tests

    [Fact]
    public async Task GetUserJournalEntriesAsync_ReturnsOnlyUserEntries()
    {
        var userId = 1;
        var otherUserId = 2;

        await Context.JournalEntries.AddRangeAsync(
            new JournalEntryBuilder().WithUserId(userId).Build(),
            new JournalEntryBuilder().WithUserId(userId).Build());
        await Context.JournalEntries.AddAsync(new JournalEntryBuilder().WithUserId(otherUserId).Build());
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserJournalEntriesAsync(userId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserJournalEntriesAsync_FiltersOutDeletedEntries()
    {
        var userId = 1;
        var active = new JournalEntryBuilder().WithUserId(userId).AsActive().Build();
        var deleted = new JournalEntryBuilder().WithUserId(userId).AsDeleted().Build();

        await Context.JournalEntries.AddAsync(active);
        await Context.JournalEntries.AddAsync(deleted);
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserJournalEntriesAsync(userId);

        result.Should().HaveCount(1);
        result.Should().NotContain(e => e.Id == deleted.Id);
    }

    [Fact]
    public async Task GetUserJournalEntriesAsync_FiltersByDateRange()
    {
        var userId = 1;
        var inRange = new JournalEntryBuilder().WithUserId(userId).WithEntryDate(new DateTime(2026, 6, 15)).Build();
        var outOfRange = new JournalEntryBuilder().WithUserId(userId).WithEntryDate(new DateTime(2026, 1, 1)).Build();

        await Context.JournalEntries.AddAsync(inRange);
        await Context.JournalEntries.AddAsync(outOfRange);
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserJournalEntriesAsync(userId, new DateTime(2026, 6, 1), new DateTime(2026, 6, 30));

        result.Should().HaveCount(1);
        result.Should().Contain(e => e.Id == inRange.Id);
    }

    #endregion

    #region UpdateJournalEntryAsync Tests

    [Fact]
    public async Task UpdateJournalEntryAsync_WithValidRequest_UpdatesSuccessfully()
    {
        var userId = 1;
        var entry = new JournalEntryBuilder().WithUserId(userId).Build();
        await Context.JournalEntries.AddAsync(entry);
        await Context.SaveChangesAsync();

        var request = new UpdateJournalEntryRequest { Content = "Updated content." };
        var result = await _sut.UpdateJournalEntryAsync(userId, entry.Id, request);

        result.Should().NotBeNull();
        result!.Content.Should().Be("Updated content.");
    }

    [Fact]
    public async Task UpdateJournalEntryAsync_WithNonExistentEntry_ReturnsNull()
    {
        var result = await _sut.UpdateJournalEntryAsync(1, 999, new UpdateJournalEntryRequest());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateJournalEntryAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var entry = new JournalEntryBuilder().WithUserId(ownerId).Build();
        await Context.JournalEntries.AddAsync(entry);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.UpdateJournalEntryAsync(otherUserId, entry.Id, new UpdateJournalEntryRequest());

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to update this journal entry");
    }

    #endregion

    #region DeleteJournalEntryAsync Tests

    [Fact]
    public async Task DeleteJournalEntryAsync_SoftDeletesEntry()
    {
        var userId = 1;
        var entry = new JournalEntryBuilder().WithUserId(userId).AsActive().Build();
        await Context.JournalEntries.AddAsync(entry);
        await Context.SaveChangesAsync();

        var result = await _sut.DeleteJournalEntryAsync(userId, entry.Id);

        result.Should().BeTrue();
        var deleted = await Context.JournalEntries.FindAsync(entry.Id);
        deleted!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteJournalEntryAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var entry = new JournalEntryBuilder().WithUserId(ownerId).Build();
        await Context.JournalEntries.AddAsync(entry);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.DeleteJournalEntryAsync(otherUserId, entry.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to delete this journal entry");
    }

    #endregion
}
