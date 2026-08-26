using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class MeditationSessionServiceTests : ServiceTestBase
{
    private readonly MeditationSessionService _sut;

    public MeditationSessionServiceTests()
    {
        _sut = new MeditationSessionService(Context);
    }

    #region CreateMeditationSessionAsync Tests

    [Fact]
    public async Task CreateMeditationSessionAsync_WithValidRequest_CreatesSuccessfully()
    {
        var userId = 1;
        var request = new CreateMeditationSessionRequest
        {
            SessionDate = DateTime.UtcNow.Date,
            DurationMinutes = 20,
            MeditationType = "Guided",
            MoodBefore = 2,
            MoodAfter = 4
        };

        var result = await _sut.CreateMeditationSessionAsync(userId, request);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.MeditationType.Should().Be("Guided");
        result.MoodBefore.Should().Be(2);
        result.MoodAfter.Should().Be(4);

        var session = await Context.MeditationSessions.FindAsync(result.Id);
        session!.Status.Should().Be(1);
    }

    [Fact]
    public async Task CreateMeditationSessionAsync_WithNullMood_Succeeds()
    {
        var userId = 1;
        var request = new CreateMeditationSessionRequest
        {
            SessionDate = DateTime.UtcNow.Date,
            DurationMinutes = 15,
            MeditationType = "Breathing"
        };

        var result = await _sut.CreateMeditationSessionAsync(userId, request);

        result.Should().NotBeNull();
        result.MoodBefore.Should().BeNull();
        result.MoodAfter.Should().BeNull();
    }

    #endregion

    #region GetUserMeditationSessionsAsync Tests

    [Fact]
    public async Task GetUserMeditationSessionsAsync_ReturnsOnlyUserSessions()
    {
        var userId = 1;
        var otherUserId = 2;

        await Context.MeditationSessions.AddRangeAsync(
            new MeditationSessionBuilder().WithUserId(userId).Build(),
            new MeditationSessionBuilder().WithUserId(userId).Build());
        await Context.MeditationSessions.AddAsync(new MeditationSessionBuilder().WithUserId(otherUserId).Build());
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserMeditationSessionsAsync(userId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserMeditationSessionsAsync_FiltersOutDeletedSessions()
    {
        var userId = 1;
        var active = new MeditationSessionBuilder().WithUserId(userId).AsActive().Build();
        var deleted = new MeditationSessionBuilder().WithUserId(userId).AsDeleted().Build();

        await Context.MeditationSessions.AddAsync(active);
        await Context.MeditationSessions.AddAsync(deleted);
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserMeditationSessionsAsync(userId);

        result.Should().HaveCount(1);
        result.Should().NotContain(s => s.Id == deleted.Id);
    }

    [Fact]
    public async Task GetUserMeditationSessionsAsync_FiltersByDateRange()
    {
        var userId = 1;
        var inRange = new MeditationSessionBuilder().WithUserId(userId).WithSessionDate(new DateTime(2026, 6, 15)).Build();
        var outOfRange = new MeditationSessionBuilder().WithUserId(userId).WithSessionDate(new DateTime(2026, 1, 1)).Build();

        await Context.MeditationSessions.AddAsync(inRange);
        await Context.MeditationSessions.AddAsync(outOfRange);
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserMeditationSessionsAsync(userId, new DateTime(2026, 6, 1), new DateTime(2026, 6, 30));

        result.Should().HaveCount(1);
        result.Should().Contain(s => s.Id == inRange.Id);
    }

    #endregion

    #region UpdateMeditationSessionAsync Tests

    [Fact]
    public async Task UpdateMeditationSessionAsync_WithValidRequest_UpdatesSuccessfully()
    {
        var userId = 1;
        var session = new MeditationSessionBuilder().WithUserId(userId).Build();
        await Context.MeditationSessions.AddAsync(session);
        await Context.SaveChangesAsync();

        var request = new UpdateMeditationSessionRequest { DurationMinutes = 45 };
        var result = await _sut.UpdateMeditationSessionAsync(userId, session.Id, request);

        result.Should().NotBeNull();
        result!.DurationMinutes.Should().Be(45);
    }

    [Fact]
    public async Task UpdateMeditationSessionAsync_WithNonExistentSession_ReturnsNull()
    {
        var result = await _sut.UpdateMeditationSessionAsync(1, 999, new UpdateMeditationSessionRequest());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateMeditationSessionAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var session = new MeditationSessionBuilder().WithUserId(ownerId).Build();
        await Context.MeditationSessions.AddAsync(session);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.UpdateMeditationSessionAsync(otherUserId, session.Id, new UpdateMeditationSessionRequest());

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to update this meditation session");
    }

    #endregion

    #region DeleteMeditationSessionAsync Tests

    [Fact]
    public async Task DeleteMeditationSessionAsync_SoftDeletesSession()
    {
        var userId = 1;
        var session = new MeditationSessionBuilder().WithUserId(userId).AsActive().Build();
        await Context.MeditationSessions.AddAsync(session);
        await Context.SaveChangesAsync();

        var result = await _sut.DeleteMeditationSessionAsync(userId, session.Id);

        result.Should().BeTrue();
        var deleted = await Context.MeditationSessions.FindAsync(session.Id);
        deleted!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteMeditationSessionAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var session = new MeditationSessionBuilder().WithUserId(ownerId).Build();
        await Context.MeditationSessions.AddAsync(session);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.DeleteMeditationSessionAsync(otherUserId, session.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to delete this meditation session");
    }

    #endregion
}
