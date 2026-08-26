using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class WeeklyRoutineServiceTests : ServiceTestBase
{
    private readonly WeeklyRoutineService _sut;

    public WeeklyRoutineServiceTests()
    {
        _sut = new WeeklyRoutineService(Context);
    }

    #region CreateWeeklyRoutineAsync Tests

    [Fact]
    public async Task CreateWeeklyRoutineAsync_WithValidRequest_CreatesSuccessfully()
    {
        var userId = 1;
        var request = new CreateWeeklyRoutineRequest
        {
            DayOfWeek = 1,
            RoutineName = "Push Day",
            Description = "Chest, shoulders, triceps"
        };

        var result = await _sut.CreateWeeklyRoutineAsync(userId, request);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.DayOfWeek.Should().Be(1);
        result.RoutineName.Should().Be("Push Day");

        var routine = await Context.WeeklyRoutines.FindAsync(result.Id);
        routine.Should().NotBeNull();
        routine!.Status.Should().Be(1);
    }

    #endregion

    #region GetUserWeeklyRoutinesAsync Tests

    [Fact]
    public async Task GetUserWeeklyRoutinesAsync_ReturnsOnlyUserRoutines()
    {
        var userId = 1;
        var otherUserId = 2;

        await Context.WeeklyRoutines.AddRangeAsync(
            new WeeklyRoutineBuilder().WithUserId(userId).WithDayOfWeek(1).Build(),
            new WeeklyRoutineBuilder().WithUserId(userId).WithDayOfWeek(2).Build(),
            new WeeklyRoutineBuilder().WithUserId(otherUserId).WithDayOfWeek(1).Build());
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserWeeklyRoutinesAsync(userId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserWeeklyRoutinesAsync_FiltersOutDeletedRoutines()
    {
        var userId = 1;
        var active = new WeeklyRoutineBuilder().WithUserId(userId).WithDayOfWeek(1).AsActive().Build();
        var deleted = new WeeklyRoutineBuilder().WithUserId(userId).WithDayOfWeek(2).AsDeleted().Build();

        await Context.WeeklyRoutines.AddAsync(active);
        await Context.WeeklyRoutines.AddAsync(deleted);
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserWeeklyRoutinesAsync(userId);

        result.Should().HaveCount(1);
        result.Should().NotContain(r => r.Id == deleted.Id);
    }

    #endregion

    #region UpdateWeeklyRoutineAsync Tests

    [Fact]
    public async Task UpdateWeeklyRoutineAsync_WithValidRequest_UpdatesSuccessfully()
    {
        var userId = 1;
        var routine = new WeeklyRoutineBuilder().WithUserId(userId).WithRoutineName("Push Day").Build();
        await Context.WeeklyRoutines.AddAsync(routine);
        await Context.SaveChangesAsync();

        var request = new UpdateWeeklyRoutineRequest { RoutineName = "Updated Push Day" };
        var result = await _sut.UpdateWeeklyRoutineAsync(userId, routine.Id, request);

        result.Should().NotBeNull();
        result!.RoutineName.Should().Be("Updated Push Day");
    }

    [Fact]
    public async Task UpdateWeeklyRoutineAsync_WithNonExistentRoutine_ReturnsNull()
    {
        var result = await _sut.UpdateWeeklyRoutineAsync(1, 999, new UpdateWeeklyRoutineRequest());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateWeeklyRoutineAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var routine = new WeeklyRoutineBuilder().WithUserId(ownerId).Build();
        await Context.WeeklyRoutines.AddAsync(routine);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.UpdateWeeklyRoutineAsync(otherUserId, routine.Id, new UpdateWeeklyRoutineRequest());

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to update this weekly routine");
    }

    #endregion

    #region DeleteWeeklyRoutineAsync Tests

    [Fact]
    public async Task DeleteWeeklyRoutineAsync_SoftDeletesRoutine()
    {
        var userId = 1;
        var routine = new WeeklyRoutineBuilder().WithUserId(userId).AsActive().Build();
        await Context.WeeklyRoutines.AddAsync(routine);
        await Context.SaveChangesAsync();

        var result = await _sut.DeleteWeeklyRoutineAsync(userId, routine.Id);

        result.Should().BeTrue();
        var deleted = await Context.WeeklyRoutines.FindAsync(routine.Id);
        deleted!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteWeeklyRoutineAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var routine = new WeeklyRoutineBuilder().WithUserId(ownerId).Build();
        await Context.WeeklyRoutines.AddAsync(routine);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.DeleteWeeklyRoutineAsync(otherUserId, routine.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to delete this weekly routine");
    }

    #endregion
}
