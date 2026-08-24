using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class ExpensesControllerTests : ControllerTestBase
{
    private readonly Mock<IExpenseService> _expenseServiceMock;
    private readonly ExpensesController _sut;

    public ExpensesControllerTests()
    {
        _expenseServiceMock = new Mock<IExpenseService>();
        _sut = new ExpensesController(_expenseServiceMock.Object);
    }

    #region GetExpenses Tests

    [Fact]
    public async Task GetExpenses_WhenUserOwnsResource_Returns200OkWithExpenses()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expectedExpenses = new List<ExpenseResponse>
        {
            new ExpenseResponse { Id = 1, UserId = userId, Category = "Food", Amount = 50.00m },
            new ExpenseResponse { Id = 2, UserId = userId, Category = "Transport", Amount = 30.00m }
        };

        _expenseServiceMock
            .Setup(x => x.GetUserExpensesAsync(userId, null, null, null))
            .ReturnsAsync(expectedExpenses);

        // Act
        var result = await _sut.GetExpenses(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedExpenses);
    }

    [Fact]
    public async Task GetExpenses_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        // Act
        var result = await _sut.GetExpenses(userId: 2);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetExpenses_WithFilters_Returns200OkWithFilteredExpenses()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        const string category = "Food";

        var expectedExpenses = new List<ExpenseResponse>
        {
            new ExpenseResponse { Id = 1, UserId = userId, Category = category, Amount = 50.00m }
        };

        _expenseServiceMock
            .Setup(x => x.GetUserExpensesAsync(userId, startDate, endDate, category))
            .ReturnsAsync(expectedExpenses);

        // Act
        var result = await _sut.GetExpenses(userId, startDate, endDate, category);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedExpenses);
    }

    [Fact]
    public async Task GetExpenses_WhenNoExpensesExist_Returns200OkWithEmptyList()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _expenseServiceMock
            .Setup(x => x.GetUserExpensesAsync(userId, null, null, null))
            .ReturnsAsync(new List<ExpenseResponse>());

        // Act
        var result = await _sut.GetExpenses(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var expenses = okResult.Value.Should().BeOfType<List<ExpenseResponse>>().Subject;
        expenses.Should().BeEmpty();
    }

    #endregion

    #region CreateExpense Tests

    [Fact]
    public async Task CreateExpense_WhenUserOwnsResource_Returns201CreatedWithExpense()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreateExpenseRequest
        {
            Category = "Food",
            PaymentMethod = "Credit Card",
            CurrencyCode = "USD",
            Amount = 50.00m,
            Description = "Grocery shopping",
            ExpenseDate = DateTime.UtcNow
        };

        var expectedResponse = new ExpenseResponse
        {
            Id = 1,
            UserId = userId,
            Category = request.Category,
            PaymentMethod = request.PaymentMethod,
            CurrencyCode = request.CurrencyCode,
            Amount = request.Amount,
            Description = request.Description,
            ExpenseDate = request.ExpenseDate
        };

        _expenseServiceMock
            .Setup(x => x.CreateExpenseAsync(userId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CreateExpense(userId, request);

        // Assert
        var createdResult = result.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateExpense_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        var request = new CreateExpenseRequest
        {
            Category = "Food",
            PaymentMethod = "Credit Card",
            CurrencyCode = "USD",
            Amount = 50.00m,
            ExpenseDate = DateTime.UtcNow
        };

        // Act
        var result = await _sut.CreateExpense(userId: 2, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateExpense Tests

    [Fact]
    public async Task UpdateExpense_WhenUserOwnsResourceAndExpenseExists_Returns200OkWithUpdatedExpense()
    {
        // Arrange
        const int userId = 1;
        const int expenseId = 1;
        SetupControllerContext(_sut, userId);

        var request = new UpdateExpenseRequest
        {
            Category = "Transport",
            Amount = 75.00m
        };

        var expectedResponse = new ExpenseResponse
        {
            Id = expenseId,
            UserId = userId,
            Category = "Transport",
            Amount = 75.00m
        };

        _expenseServiceMock
            .Setup(x => x.UpdateExpenseAsync(userId, expenseId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.UpdateExpense(userId, expenseId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UpdateExpense_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        var request = new UpdateExpenseRequest
        {
            Amount = 75.00m
        };

        // Act
        var result = await _sut.UpdateExpense(userId: 2, expenseId: 1, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateExpense_WhenExpenseDoesNotExist_Returns404NotFound()
    {
        // Arrange
        const int userId = 1;
        const int expenseId = 999;
        SetupControllerContext(_sut, userId);

        var request = new UpdateExpenseRequest
        {
            Amount = 75.00m
        };

        _expenseServiceMock
            .Setup(x => x.UpdateExpenseAsync(userId, expenseId, request))
            .ReturnsAsync((ExpenseResponse?)null);

        // Act
        var result = await _sut.UpdateExpense(userId, expenseId, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateExpense_WhenExpenseBelongsToDifferentUser_Returns403Forbidden()
    {
        // Arrange
        const int userId = 1;
        const int expenseId = 1;
        SetupControllerContext(_sut, userId);

        var request = new UpdateExpenseRequest
        {
            Amount = 75.00m
        };

        _expenseServiceMock
            .Setup(x => x.UpdateExpenseAsync(userId, expenseId, request))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _sut.UpdateExpense(userId, expenseId, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
