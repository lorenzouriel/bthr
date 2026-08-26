using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

// Note: EF Core's InMemory provider (used by ServiceTestBase) does not execute
// Postgres's generated-column SQL for sleep_logs.total_hours, so these tests do
// not assert a specific computed value for TotalHours — that guarantee is
// covered by DESIGN_BODY_MODULE_API.md's Decision 1 (live-verified against real
// Postgres) and by Build's own live re-verification. See Decision 4.
public class SleepLogServiceTests : ServiceTestBase
{
    private readonly SleepLogService _sut;

    public SleepLogServiceTests()
    {
        _sut = new SleepLogService(Context);
    }

    #region CreateSleepLogAsync Tests

    [Fact]
    public async Task CreateSleepLogAsync_WithValidRequest_CreatesSuccessfully()
    {
        var userId = 1;
        var bedTime = new DateTime(2026, 8, 24, 23, 0, 0, DateTimeKind.Utc);
        var wakeTime = new DateTime(2026, 8, 25, 6, 30, 0, DateTimeKind.Utc);

        var request = new CreateSleepLogRequest
        {
            BedTime = bedTime,
            WakeTime = wakeTime,
            Notes = "Slept well"
        };

        var result = await _sut.CreateSleepLogAsync(userId, request);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.BedTime.Should().Be(bedTime);
        result.WakeTime.Should().Be(wakeTime);

        var sleepLog = await Context.SleepLogs.FindAsync(result.Id);
        sleepLog!.Status.Should().Be(1);
    }

    #endregion

    #region GetUserSleepLogsAsync Tests

    [Fact]
    public async Task GetUserSleepLogsAsync_ReturnsOnlyUserLogs()
    {
        var userId = 1;
        var otherUserId = 2;

        await Context.SleepLogs.AddRangeAsync(
            new SleepLogBuilder().WithUserId(userId).Build(),
            new SleepLogBuilder().WithUserId(userId).Build());
        await Context.SleepLogs.AddAsync(new SleepLogBuilder().WithUserId(otherUserId).Build());
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserSleepLogsAsync(userId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(s => s.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserSleepLogsAsync_FiltersOutDeletedLogs()
    {
        var userId = 1;
        var active = new SleepLogBuilder().WithUserId(userId).AsActive().Build();
        var deleted = new SleepLogBuilder().WithUserId(userId).AsDeleted().Build();

        await Context.SleepLogs.AddAsync(active);
        await Context.SleepLogs.AddAsync(deleted);
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserSleepLogsAsync(userId);

        result.Should().HaveCount(1);
        result.Should().NotContain(s => s.Id == deleted.Id);
    }

    #endregion

    #region UpdateSleepLogAsync Tests

    [Fact]
    public async Task UpdateSleepLogAsync_WithValidRequest_UpdatesSuccessfully()
    {
        var userId = 1;
        var sleepLog = new SleepLogBuilder().WithUserId(userId).Build();
        await Context.SleepLogs.AddAsync(sleepLog);
        await Context.SaveChangesAsync();

        var request = new UpdateSleepLogRequest { Notes = "Updated notes" };
        var result = await _sut.UpdateSleepLogAsync(userId, sleepLog.Id, request);

        result.Should().NotBeNull();
        result!.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task UpdateSleepLogAsync_WithNonExistentLog_ReturnsNull()
    {
        var result = await _sut.UpdateSleepLogAsync(1, 999, new UpdateSleepLogRequest());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSleepLogAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var sleepLog = new SleepLogBuilder().WithUserId(ownerId).Build();
        await Context.SleepLogs.AddAsync(sleepLog);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.UpdateSleepLogAsync(otherUserId, sleepLog.Id, new UpdateSleepLogRequest());

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to update this sleep log");
    }

    #endregion

    #region DeleteSleepLogAsync Tests

    [Fact]
    public async Task DeleteSleepLogAsync_SoftDeletesLog()
    {
        var userId = 1;
        var sleepLog = new SleepLogBuilder().WithUserId(userId).AsActive().Build();
        await Context.SleepLogs.AddAsync(sleepLog);
        await Context.SaveChangesAsync();

        var result = await _sut.DeleteSleepLogAsync(userId, sleepLog.Id);

        result.Should().BeTrue();
        var deleted = await Context.SleepLogs.FindAsync(sleepLog.Id);
        deleted!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteSleepLogAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var sleepLog = new SleepLogBuilder().WithUserId(ownerId).Build();
        await Context.SleepLogs.AddAsync(sleepLog);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.DeleteSleepLogAsync(otherUserId, sleepLog.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to delete this sleep log");
    }

    #endregion
}
