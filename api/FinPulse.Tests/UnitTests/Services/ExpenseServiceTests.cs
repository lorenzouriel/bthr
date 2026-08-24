using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class ExpenseServiceTests : ServiceTestBase
{
    private readonly ExpenseService _sut;

    public ExpenseServiceTests()
    {
        _sut = new ExpenseService(Context);
    }

    #region CreateExpenseAsync Tests

    [Fact]
    public async Task CreateExpenseAsync_WithValidRequest_CreatesExpenseSuccessfully()
    {
        // Arrange
        var userId = 1;
        var request = new CreateExpenseRequest
        {
            Category = "Food",
            PaymentMethod = "Credit Card",
            CurrencyCode = "USD",
            Amount = 50.00m,
            Description = "Grocery shopping",
            ExpenseDate = DateTime.UtcNow.Date
        };

        // Act
        var result = await _sut.CreateExpenseAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.Category.Should().Be(request.Category);
        result.PaymentMethod.Should().Be(request.PaymentMethod);
        result.Amount.Should().Be(request.Amount);
        result.Description.Should().Be(request.Description);
        result.ExpenseDate.Should().Be(request.ExpenseDate);

        var expense = await Context.Expenses.FindAsync(result.Id);
        expense.Should().NotBeNull();
        expense!.Status.Should().Be(1);
    }

    #endregion

    #region GetUserExpensesAsync Tests

    [Fact]
    public async Task GetUserExpensesAsync_ReturnsOnlyUserExpenses()
    {
        // Arrange
        var userId = 1;
        var otherUserId = 2;

        var userExpenses = new[]
        {
            new ExpenseBuilder().WithUserId(userId).Build(),
            new ExpenseBuilder().WithUserId(userId).Build()
        };
        var otherUserExpense = new ExpenseBuilder().WithUserId(otherUserId).Build();

        await Context.Expenses.AddRangeAsync(userExpenses);
        await Context.Expenses.AddAsync(otherUserExpense);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserExpensesAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserExpensesAsync_FiltersOutDeletedExpenses()
    {
        // Arrange
        var userId = 1;
        var activeExpense = new ExpenseBuilder().WithUserId(userId).AsActive().Build();
        var deletedExpense = new ExpenseBuilder().WithUserId(userId).AsDeleted().Build();

        await Context.Expenses.AddAsync(activeExpense);
        await Context.Expenses.AddAsync(deletedExpense);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserExpensesAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().NotContain(e => e.Id == deletedExpense.Id);
    }

    [Fact]
    public async Task GetUserExpensesAsync_FiltersByDateRange()
    {
        // Arrange
        var userId = 1;
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        var expenseInRange = new ExpenseBuilder()
            .WithUserId(userId)
            .WithExpenseDate(new DateTime(2024, 1, 15))
            .Build();
        var expenseBeforeRange = new ExpenseBuilder()
            .WithUserId(userId)
            .WithExpenseDate(new DateTime(2023, 12, 31))
            .Build();
        var expenseAfterRange = new ExpenseBuilder()
            .WithUserId(userId)
            .WithExpenseDate(new DateTime(2024, 2, 1))
            .Build();

        await Context.Expenses.AddRangeAsync(expenseInRange, expenseBeforeRange, expenseAfterRange);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserExpensesAsync(userId, startDate, endDate);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(expenseInRange.Id);
    }

    [Fact]
    public async Task GetUserExpensesAsync_FiltersByCategory()
    {
        // Arrange
        var userId = 1;
        var category = "Food";

        var foodExpense = new ExpenseBuilder()
            .WithUserId(userId)
            .WithCategory("Food")
            .Build();
        var transportExpense = new ExpenseBuilder()
            .WithUserId(userId)
            .WithCategory("Transport")
            .Build();

        await Context.Expenses.AddRangeAsync(foodExpense, transportExpense);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserExpensesAsync(userId, category: category);

        // Assert
        result.Should().HaveCount(1);
        result[0].Category.Should().Be("Food");
    }

    [Fact]
    public async Task GetUserExpensesAsync_WhenNoExpenses_ReturnsEmptyList()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _sut.GetUserExpensesAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateExpenseAsync Tests

    [Fact]
    public async Task UpdateExpenseAsync_WithValidRequest_UpdatesExpenseSuccessfully()
    {
        // Arrange
        var userId = 1;
        var expense = new ExpenseBuilder()
            .WithUserId(userId)
            .WithCategory("Food")
            .WithAmount(50.00m)
            .Build();
        await Context.Expenses.AddAsync(expense);
        await Context.SaveChangesAsync();

        var request = new UpdateExpenseRequest
        {
            Category = "Transport",
            Amount = 75.00m,
            Description = "Updated description"
        };

        // Act
        var result = await _sut.UpdateExpenseAsync(userId, expense.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Category.Should().Be("Transport");
        result.Amount.Should().Be(75.00m);
        result.Description.Should().Be("Updated description");

        var updatedExpense = await Context.Expenses.FindAsync(expense.Id);
        updatedExpense!.Category.Should().Be("Transport");
        updatedExpense.Amount.Should().Be(75.00m);
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithNonExistentExpense_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateExpenseRequest
        {
            Amount = 100.00m
        };

        // Act
        var result = await _sut.UpdateExpenseAsync(userId, 999, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithDeletedExpense_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var deletedExpense = new ExpenseBuilder()
            .WithUserId(userId)
            .AsDeleted()
            .Build();
        await Context.Expenses.AddAsync(deletedExpense);
        await Context.SaveChangesAsync();

        var request = new UpdateExpenseRequest
        {
            Amount = 100.00m
        };

        // Act
        var result = await _sut.UpdateExpenseAsync(userId, deletedExpense.Id, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateExpenseAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = 1;
        var otherUserId = 2;
        var expense = new ExpenseBuilder()
            .WithUserId(ownerId)
            .Build();
        await Context.Expenses.AddAsync(expense);
        await Context.SaveChangesAsync();

        var request = new UpdateExpenseRequest
        {
            Amount = 100.00m
        };

        // Act
        var act = async () => await _sut.UpdateExpenseAsync(otherUserId, expense.Id, request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to update this expense");
    }

    #endregion

    #region DeleteExpenseAsync Tests

    [Fact]
    public async Task DeleteExpenseAsync_SoftDeletesExpense()
    {
        // Arrange
        var userId = 1;
        var expense = new ExpenseBuilder()
            .WithUserId(userId)
            .AsActive()
            .Build();
        await Context.Expenses.AddAsync(expense);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteExpenseAsync(userId, expense.Id);

        // Assert
        result.Should().BeTrue();

        var deletedExpense = await Context.Expenses.FindAsync(expense.Id);
        deletedExpense!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteExpenseAsync_WithNonExistentExpense_ReturnsFalse()
    {
        // Act
        var result = await _sut.DeleteExpenseAsync(1, 999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteExpenseAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = 1;
        var otherUserId = 2;
        var expense = new ExpenseBuilder()
            .WithUserId(ownerId)
            .Build();
        await Context.Expenses.AddAsync(expense);
        await Context.SaveChangesAsync();

        // Act
        var act = async () => await _sut.DeleteExpenseAsync(otherUserId, expense.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to delete this expense");
    }

    #endregion
}
