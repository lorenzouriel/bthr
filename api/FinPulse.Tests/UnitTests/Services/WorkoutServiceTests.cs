using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class WorkoutServiceTests : ServiceTestBase
{
    private readonly WorkoutService _sut;

    public WorkoutServiceTests()
    {
        _sut = new WorkoutService(Context);
    }

    #region CreateWorkoutAsync Tests

    [Fact]
    public async Task CreateWorkoutAsync_WithValidRequest_CreatesSuccessfully()
    {
        var userId = 1;
        var request = new CreateWorkoutRequest
        {
            WorkoutDate = DateTime.UtcNow.Date,
            RoutineName = "Push Day",
            DurationMinutes = 45,
            CaloriesBurned = 400m
        };

        var result = await _sut.CreateWorkoutAsync(userId, request);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.RoutineName.Should().Be("Push Day");

        var workout = await Context.Workouts.FindAsync(result.Id);
        workout!.Status.Should().Be(1);
    }

    #endregion

    #region GetUserWorkoutsAsync Tests

    [Fact]
    public async Task GetUserWorkoutsAsync_ReturnsOnlyUserWorkouts()
    {
        var userId = 1;
        var otherUserId = 2;

        await Context.Workouts.AddRangeAsync(
            new WorkoutBuilder().WithUserId(userId).Build(),
            new WorkoutBuilder().WithUserId(userId).Build());
        await Context.Workouts.AddAsync(new WorkoutBuilder().WithUserId(otherUserId).Build());
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserWorkoutsAsync(userId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(w => w.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserWorkoutsAsync_FiltersOutDeletedWorkouts()
    {
        var userId = 1;
        var active = new WorkoutBuilder().WithUserId(userId).AsActive().Build();
        var deleted = new WorkoutBuilder().WithUserId(userId).AsDeleted().Build();

        await Context.Workouts.AddAsync(active);
        await Context.Workouts.AddAsync(deleted);
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserWorkoutsAsync(userId);

        result.Should().HaveCount(1);
        result.Should().NotContain(w => w.Id == deleted.Id);
    }

    [Fact]
    public async Task GetUserWorkoutsAsync_FiltersByDateRange()
    {
        var userId = 1;
        var startDate = new DateTime(2026, 1, 1);
        var endDate = new DateTime(2026, 12, 31);

        var inRange = new WorkoutBuilder().WithUserId(userId).WithWorkoutDate(new DateTime(2026, 6, 1)).Build();
        var beforeRange = new WorkoutBuilder().WithUserId(userId).WithWorkoutDate(new DateTime(2025, 1, 1)).Build();

        await Context.Workouts.AddRangeAsync(inRange, beforeRange);
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserWorkoutsAsync(userId, startDate, endDate);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(inRange.Id);
    }

    #endregion

    #region UpdateWorkoutAsync Tests

    [Fact]
    public async Task UpdateWorkoutAsync_WithValidRequest_UpdatesSuccessfully()
    {
        var userId = 1;
        var workout = new WorkoutBuilder().WithUserId(userId).Build();
        await Context.Workouts.AddAsync(workout);
        await Context.SaveChangesAsync();

        var request = new UpdateWorkoutRequest { RoutineName = "Updated Leg Day" };
        var result = await _sut.UpdateWorkoutAsync(userId, workout.Id, request);

        result.Should().NotBeNull();
        result!.RoutineName.Should().Be("Updated Leg Day");
    }

    [Fact]
    public async Task UpdateWorkoutAsync_WithNonExistentWorkout_ReturnsNull()
    {
        var result = await _sut.UpdateWorkoutAsync(1, 999, new UpdateWorkoutRequest());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateWorkoutAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var workout = new WorkoutBuilder().WithUserId(ownerId).Build();
        await Context.Workouts.AddAsync(workout);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.UpdateWorkoutAsync(otherUserId, workout.Id, new UpdateWorkoutRequest());

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to update this workout");
    }

    #endregion

    #region DeleteWorkoutAsync Tests

    [Fact]
    public async Task DeleteWorkoutAsync_SoftDeletesWorkout()
    {
        var userId = 1;
        var workout = new WorkoutBuilder().WithUserId(userId).AsActive().Build();
        await Context.Workouts.AddAsync(workout);
        await Context.SaveChangesAsync();

        var result = await _sut.DeleteWorkoutAsync(userId, workout.Id);

        result.Should().BeTrue();
        var deleted = await Context.Workouts.FindAsync(workout.Id);
        deleted!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteWorkoutAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var workout = new WorkoutBuilder().WithUserId(ownerId).Build();
        await Context.Workouts.AddAsync(workout);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.DeleteWorkoutAsync(otherUserId, workout.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to delete this workout");
    }

    #endregion
}
