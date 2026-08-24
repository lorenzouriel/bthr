using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class GoalServiceTests : ServiceTestBase
{
    private readonly GoalService _sut;

    public GoalServiceTests()
    {
        _sut = new GoalService(Context);
    }

    #region CreateGoalAsync Tests

    [Fact]
    public async Task CreateGoalAsync_WithValidRequest_CreatesGoalSuccessfully()
    {
        // Arrange
        var userId = 1;
        var request = new CreateGoalRequest
        {
            Name = "Emergency Fund",
            Description = "Save for 6 months of expenses",
            TargetAmount = 10000.00m,
            CurrentAmount = 2000.00m,
            CurrencyCode = "USD",
            DueDate = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var result = await _sut.CreateGoalAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.Name.Should().Be(request.Name);
        result.Description.Should().Be(request.Description);
        result.TargetAmount.Should().Be(request.TargetAmount);
        result.CurrentAmount.Should().Be(request.CurrentAmount);
        result.DueDate.Should().Be(request.DueDate);

        var goal = await Context.Goals.FindAsync(result.Id);
        goal.Should().NotBeNull();
        goal!.Status.Should().Be(1);
    }

    #endregion

    #region GetUserGoalsAsync Tests

    [Fact]
    public async Task GetUserGoalsAsync_ReturnsOnlyUserGoals()
    {
        // Arrange
        var userId = 1;
        var otherUserId = 2;

        var userGoals = new[]
        {
            new GoalBuilder().WithUserId(userId).Build(),
            new GoalBuilder().WithUserId(userId).Build()
        };
        var otherUserGoal = new GoalBuilder().WithUserId(otherUserId).Build();

        await Context.Goals.AddRangeAsync(userGoals);
        await Context.Goals.AddAsync(otherUserGoal);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserGoalsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(g => g.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserGoalsAsync_FiltersOutDeletedGoals()
    {
        // Arrange
        var userId = 1;
        var activeGoal = new GoalBuilder().WithUserId(userId).AsActive().Build();
        var deletedGoal = new GoalBuilder().WithUserId(userId).AsDeleted().Build();

        await Context.Goals.AddAsync(activeGoal);
        await Context.Goals.AddAsync(deletedGoal);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserGoalsAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().NotContain(g => g.Id == deletedGoal.Id);
    }

    [Fact]
    public async Task GetUserGoalsAsync_FiltersByStartDate()
    {
        // Arrange
        var userId = 1;
        var filterStartDate = new DateTime(2024, 6, 1);

        var goalAfterStartDate = new GoalBuilder()
            .WithUserId(userId)
            .WithDueDate(new DateTime(2024, 12, 31))
            .Build();
        var goalBeforeStartDate = new GoalBuilder()
            .WithUserId(userId)
            .WithDueDate(new DateTime(2024, 5, 1))
            .Build();

        await Context.Goals.AddRangeAsync(goalAfterStartDate, goalBeforeStartDate);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserGoalsAsync(userId, startDate: filterStartDate);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(goalAfterStartDate.Id);
    }

    [Fact]
    public async Task GetUserGoalsAsync_FiltersByEndDate()
    {
        // Arrange
        var userId = 1;
        var filterEndDate = new DateTime(2024, 12, 31);

        var goalWithinEndDate = new GoalBuilder()
            .WithUserId(userId)
            .WithDueDate(new DateTime(2024, 6, 30))
            .Build();
        var goalAfterEndDate = new GoalBuilder()
            .WithUserId(userId)
            .WithDueDate(new DateTime(2025, 6, 30))
            .Build();

        await Context.Goals.AddRangeAsync(goalWithinEndDate, goalAfterEndDate);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserGoalsAsync(userId, endDate: filterEndDate);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(goalWithinEndDate.Id);
    }

    [Fact]
    public async Task GetUserGoalsAsync_FiltersByDateRange()
    {
        // Arrange
        var userId = 1;
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        var goalInRange = new GoalBuilder()
            .WithUserId(userId)
            .WithDueDate(new DateTime(2024, 6, 30))
            .Build();
        var goalBeforeRange = new GoalBuilder()
            .WithUserId(userId)
            .WithDueDate(new DateTime(2023, 12, 31))
            .Build();
        var goalAfterRange = new GoalBuilder()
            .WithUserId(userId)
            .WithDueDate(new DateTime(2025, 1, 1))
            .Build();

        await Context.Goals.AddRangeAsync(goalInRange, goalBeforeRange, goalAfterRange);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserGoalsAsync(userId, startDate, endDate);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(goalInRange.Id);
    }

    [Fact]
    public async Task GetUserGoalsAsync_WhenNoGoals_ReturnsEmptyList()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _sut.GetUserGoalsAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateGoalAsync Tests

    [Fact]
    public async Task UpdateGoalAsync_WithValidRequest_UpdatesGoalSuccessfully()
    {
        // Arrange
        var userId = 1;
        var goal = new GoalBuilder()
            .WithUserId(userId)
            .WithName("Emergency Fund")
            .WithTargetAmount(10000.00m)
            .WithCurrentAmount(2000.00m)
            .Build();
        await Context.Goals.AddAsync(goal);
        await Context.SaveChangesAsync();

        var request = new UpdateGoalRequest
        {
            Name = "Updated Emergency Fund",
            TargetAmount = 15000.00m,
            CurrentAmount = 5000.00m,
            Description = "Updated description"
        };

        // Act
        var result = await _sut.UpdateGoalAsync(userId, goal.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Emergency Fund");
        result.TargetAmount.Should().Be(15000.00m);
        result.CurrentAmount.Should().Be(5000.00m);
        result.Description.Should().Be("Updated description");

        var updatedGoal = await Context.Goals.FindAsync(goal.Id);
        updatedGoal!.Name.Should().Be("Updated Emergency Fund");
        updatedGoal.TargetAmount.Should().Be(15000.00m);
        updatedGoal.CurrentAmount.Should().Be(5000.00m);
    }

    [Fact]
    public async Task UpdateGoalAsync_WithNonExistentGoal_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateGoalRequest
        {
            CurrentAmount = 5000.00m
        };

        // Act
        var result = await _sut.UpdateGoalAsync(userId, 999, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateGoalAsync_WithDeletedGoal_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var deletedGoal = new GoalBuilder()
            .WithUserId(userId)
            .AsDeleted()
            .Build();
        await Context.Goals.AddAsync(deletedGoal);
        await Context.SaveChangesAsync();

        var request = new UpdateGoalRequest
        {
            CurrentAmount = 5000.00m
        };

        // Act
        var result = await _sut.UpdateGoalAsync(userId, deletedGoal.Id, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateGoalAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = 1;
        var otherUserId = 2;
        var goal = new GoalBuilder()
            .WithUserId(ownerId)
            .Build();
        await Context.Goals.AddAsync(goal);
        await Context.SaveChangesAsync();

        var request = new UpdateGoalRequest
        {
            CurrentAmount = 5000.00m
        };

        // Act
        var act = async () => await _sut.UpdateGoalAsync(otherUserId, goal.Id, request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to update this goal");
    }

    #endregion

    #region DeleteGoalAsync Tests

    [Fact]
    public async Task DeleteGoalAsync_SoftDeletesGoal()
    {
        // Arrange
        var userId = 1;
        var goal = new GoalBuilder()
            .WithUserId(userId)
            .AsActive()
            .Build();
        await Context.Goals.AddAsync(goal);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteGoalAsync(userId, goal.Id);

        // Assert
        result.Should().BeTrue();

        var deletedGoal = await Context.Goals.FindAsync(goal.Id);
        deletedGoal!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteGoalAsync_WithNonExistentGoal_ReturnsFalse()
    {
        // Act
        var result = await _sut.DeleteGoalAsync(1, 999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteGoalAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = 1;
        var otherUserId = 2;
        var goal = new GoalBuilder()
            .WithUserId(ownerId)
            .Build();
        await Context.Goals.AddAsync(goal);
        await Context.SaveChangesAsync();

        // Act
        var act = async () => await _sut.DeleteGoalAsync(otherUserId, goal.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to delete this goal");
    }

    #endregion
}
