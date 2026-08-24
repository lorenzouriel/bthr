using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class BudgetServiceTests : ServiceTestBase
{
    private readonly BudgetService _sut;

    public BudgetServiceTests()
    {
        _sut = new BudgetService(Context);
    }

    #region CreateBudgetAsync Tests

    [Fact]
    public async Task CreateBudgetAsync_WithValidRequest_CreatesBudgetSuccessfully()
    {
        // Arrange
        var userId = 1;
        var request = new CreateBudgetRequest
        {
            Name = "Monthly Food Budget",
            Description = "Budget for groceries and dining",
            AmountLimit = 1000.00m,
            CurrencyCode = "USD",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 31)
        };

        // Act
        var result = await _sut.CreateBudgetAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.Name.Should().Be(request.Name);
        result.Description.Should().Be(request.Description);
        result.AmountLimit.Should().Be(request.AmountLimit);
        result.StartDate.Should().Be(request.StartDate);
        result.EndDate.Should().Be(request.EndDate);

        var budget = await Context.Budgets.FindAsync(result.Id);
        budget.Should().NotBeNull();
        budget!.Status.Should().Be(1);
    }

    #endregion

    #region GetUserBudgetsAsync Tests

    [Fact]
    public async Task GetUserBudgetsAsync_ReturnsOnlyUserBudgets()
    {
        // Arrange
        var userId = 1;
        var otherUserId = 2;

        var userBudgets = new[]
        {
            new BudgetBuilder().WithUserId(userId).Build(),
            new BudgetBuilder().WithUserId(userId).Build()
        };
        var otherUserBudget = new BudgetBuilder().WithUserId(otherUserId).Build();

        await Context.Budgets.AddRangeAsync(userBudgets);
        await Context.Budgets.AddAsync(otherUserBudget);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserBudgetsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(b => b.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserBudgetsAsync_FiltersOutDeletedBudgets()
    {
        // Arrange
        var userId = 1;
        var activeBudget = new BudgetBuilder().WithUserId(userId).AsActive().Build();
        var deletedBudget = new BudgetBuilder().WithUserId(userId).AsDeleted().Build();

        await Context.Budgets.AddAsync(activeBudget);
        await Context.Budgets.AddAsync(deletedBudget);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserBudgetsAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().NotContain(b => b.Id == deletedBudget.Id);
    }

    [Fact]
    public async Task GetUserBudgetsAsync_FiltersByStartDate()
    {
        // Arrange
        var userId = 1;
        var filterStartDate = new DateTime(2024, 1, 15);

        var budgetAfterFilter = new BudgetBuilder()
            .WithUserId(userId)
            .WithStartDate(new DateTime(2024, 1, 20))
            .Build();
        var budgetBeforeFilter = new BudgetBuilder()
            .WithUserId(userId)
            .WithStartDate(new DateTime(2024, 1, 10))
            .Build();

        await Context.Budgets.AddRangeAsync(budgetAfterFilter, budgetBeforeFilter);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserBudgetsAsync(userId, startDate: filterStartDate);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(budgetAfterFilter.Id);
    }

    [Fact]
    public async Task GetUserBudgetsAsync_FiltersByEndDate()
    {
        // Arrange
        var userId = 1;
        var filterEndDate = new DateTime(2024, 1, 31);

        var budgetWithinFilter = new BudgetBuilder()
            .WithUserId(userId)
            .WithEndDate(new DateTime(2024, 1, 25))
            .Build();
        var budgetAfterFilter = new BudgetBuilder()
            .WithUserId(userId)
            .WithEndDate(new DateTime(2024, 2, 15))
            .Build();

        await Context.Budgets.AddRangeAsync(budgetWithinFilter, budgetAfterFilter);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserBudgetsAsync(userId, endDate: filterEndDate);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(budgetWithinFilter.Id);
    }

    [Fact]
    public async Task GetUserBudgetsAsync_FiltersByDateRange()
    {
        // Arrange
        var userId = 1;
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        var budgetInRange = new BudgetBuilder()
            .WithUserId(userId)
            .WithDateRange(new DateTime(2024, 1, 10), new DateTime(2024, 1, 25))
            .Build();
        var budgetOutOfRange = new BudgetBuilder()
            .WithUserId(userId)
            .WithDateRange(new DateTime(2023, 12, 1), new DateTime(2023, 12, 31))
            .Build();

        await Context.Budgets.AddRangeAsync(budgetInRange, budgetOutOfRange);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserBudgetsAsync(userId, startDate, endDate);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(budgetInRange.Id);
    }

    [Fact]
    public async Task GetUserBudgetsAsync_WhenNoBudgets_ReturnsEmptyList()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _sut.GetUserBudgetsAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateBudgetAsync Tests

    [Fact]
    public async Task UpdateBudgetAsync_WithValidRequest_UpdatesBudgetSuccessfully()
    {
        // Arrange
        var userId = 1;
        var budget = new BudgetBuilder()
            .WithUserId(userId)
            .WithName("Food Budget")
            .WithAmountLimit(1000.00m)
            .Build();
        await Context.Budgets.AddAsync(budget);
        await Context.SaveChangesAsync();

        var request = new UpdateBudgetRequest
        {
            Name = "Updated Food Budget",
            AmountLimit = 1500.00m,
            Description = "Updated description"
        };

        // Act
        var result = await _sut.UpdateBudgetAsync(userId, budget.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Food Budget");
        result.AmountLimit.Should().Be(1500.00m);
        result.Description.Should().Be("Updated description");

        var updatedBudget = await Context.Budgets.FindAsync(budget.Id);
        updatedBudget!.Name.Should().Be("Updated Food Budget");
        updatedBudget.AmountLimit.Should().Be(1500.00m);
    }

    [Fact]
    public async Task UpdateBudgetAsync_WithNonExistentBudget_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateBudgetRequest
        {
            AmountLimit = 2000.00m
        };

        // Act
        var result = await _sut.UpdateBudgetAsync(userId, 999, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateBudgetAsync_WithDeletedBudget_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var deletedBudget = new BudgetBuilder()
            .WithUserId(userId)
            .AsDeleted()
            .Build();
        await Context.Budgets.AddAsync(deletedBudget);
        await Context.SaveChangesAsync();

        var request = new UpdateBudgetRequest
        {
            AmountLimit = 2000.00m
        };

        // Act
        var result = await _sut.UpdateBudgetAsync(userId, deletedBudget.Id, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateBudgetAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = 1;
        var otherUserId = 2;
        var budget = new BudgetBuilder()
            .WithUserId(ownerId)
            .Build();
        await Context.Budgets.AddAsync(budget);
        await Context.SaveChangesAsync();

        var request = new UpdateBudgetRequest
        {
            AmountLimit = 2000.00m
        };

        // Act
        var act = async () => await _sut.UpdateBudgetAsync(otherUserId, budget.Id, request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to update this budget");
    }

    #endregion

    #region DeleteBudgetAsync Tests

    [Fact]
    public async Task DeleteBudgetAsync_SoftDeletesBudget()
    {
        // Arrange
        var userId = 1;
        var budget = new BudgetBuilder()
            .WithUserId(userId)
            .AsActive()
            .Build();
        await Context.Budgets.AddAsync(budget);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteBudgetAsync(userId, budget.Id);

        // Assert
        result.Should().BeTrue();

        var deletedBudget = await Context.Budgets.FindAsync(budget.Id);
        deletedBudget!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteBudgetAsync_WithNonExistentBudget_ReturnsFalse()
    {
        // Act
        var result = await _sut.DeleteBudgetAsync(1, 999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBudgetAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = 1;
        var otherUserId = 2;
        var budget = new BudgetBuilder()
            .WithUserId(ownerId)
            .Build();
        await Context.Budgets.AddAsync(budget);
        await Context.SaveChangesAsync();

        // Act
        var act = async () => await _sut.DeleteBudgetAsync(otherUserId, budget.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to delete this budget");
    }

    #endregion
}
