using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class BudgetsControllerTests : ControllerTestBase
{
    private readonly Mock<IBudgetService> _budgetServiceMock;
    private readonly BudgetsController _sut;

    public BudgetsControllerTests()
    {
        _budgetServiceMock = new Mock<IBudgetService>();
        _sut = new BudgetsController(_budgetServiceMock.Object);
    }

    #region GetBudgets Tests

    [Fact]
    public async Task GetBudgets_WhenUserOwnsResource_Returns200OkWithBudgets()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expectedBudgets = new List<BudgetResponse>
        {
            new BudgetResponse { Id = 1, UserId = userId, Name = "Food Budget", AmountLimit = 500.00m },
            new BudgetResponse { Id = 2, UserId = userId, Name = "Transport Budget", AmountLimit = 200.00m }
        };

        _budgetServiceMock
            .Setup(x => x.GetUserBudgetsAsync(userId, null, null))
            .ReturnsAsync(expectedBudgets);

        // Act
        var result = await _sut.GetBudgets(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedBudgets);
    }

    [Fact]
    public async Task GetBudgets_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        // Act
        var result = await _sut.GetBudgets(userId: 2);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetBudgets_WithFilters_Returns200OkWithFilteredBudgets()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        var expectedBudgets = new List<BudgetResponse>
        {
            new BudgetResponse { Id = 1, UserId = userId, Name = "Food Budget", AmountLimit = 500.00m }
        };

        _budgetServiceMock
            .Setup(x => x.GetUserBudgetsAsync(userId, startDate, endDate))
            .ReturnsAsync(expectedBudgets);

        // Act
        var result = await _sut.GetBudgets(userId, startDate, endDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedBudgets);
    }

    [Fact]
    public async Task GetBudgets_WhenNoBudgetsExist_Returns200OkWithEmptyList()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _budgetServiceMock
            .Setup(x => x.GetUserBudgetsAsync(userId, null, null))
            .ReturnsAsync(new List<BudgetResponse>());

        // Act
        var result = await _sut.GetBudgets(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var budgets = okResult.Value.Should().BeOfType<List<BudgetResponse>>().Subject;
        budgets.Should().BeEmpty();
    }

    #endregion

    #region CreateBudget Tests

    [Fact]
    public async Task CreateBudget_WhenUserOwnsResource_Returns201CreatedWithBudget()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreateBudgetRequest
        {
            Name = "Food Budget",
            CurrencyCode = "USD",
            AmountLimit = 500.00m,
            Description = "Monthly food budget",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 31)
        };

        var expectedResponse = new BudgetResponse
        {
            Id = 1,
            UserId = userId,
            Name = request.Name,
            CurrencyCode = request.CurrencyCode,
            AmountLimit = request.AmountLimit,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        _budgetServiceMock
            .Setup(x => x.CreateBudgetAsync(userId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CreateBudget(userId, request);

        // Assert
        var createdResult = result.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateBudget_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        var request = new CreateBudgetRequest
        {
            Name = "Food Budget",
            CurrencyCode = "USD",
            AmountLimit = 500.00m,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 31)
        };

        // Act
        var result = await _sut.CreateBudget(userId: 2, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateBudget Tests

    [Fact]
    public async Task UpdateBudget_WhenUserOwnsResourceAndBudgetExists_Returns200OkWithUpdatedBudget()
    {
        // Arrange
        const int userId = 1;
        const int budgetId = 1;
        SetupControllerContext(_sut, userId);

        var request = new UpdateBudgetRequest
        {
            AmountLimit = 600.00m,
            Description = "Updated budget"
        };

        var expectedResponse = new BudgetResponse
        {
            Id = budgetId,
            UserId = userId,
            Name = "Food Budget",
            AmountLimit = 600.00m,
            Description = "Updated budget"
        };

        _budgetServiceMock
            .Setup(x => x.UpdateBudgetAsync(userId, budgetId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.UpdateBudget(userId, budgetId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UpdateBudget_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        var request = new UpdateBudgetRequest
        {
            AmountLimit = 600.00m
        };

        // Act
        var result = await _sut.UpdateBudget(userId: 2, budgetId: 1, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateBudget_WhenBudgetDoesNotExist_Returns404NotFound()
    {
        // Arrange
        const int userId = 1;
        const int budgetId = 999;
        SetupControllerContext(_sut, userId);

        var request = new UpdateBudgetRequest
        {
            AmountLimit = 600.00m
        };

        _budgetServiceMock
            .Setup(x => x.UpdateBudgetAsync(userId, budgetId, request))
            .ReturnsAsync((BudgetResponse?)null);

        // Act
        var result = await _sut.UpdateBudget(userId, budgetId, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateBudget_WhenBudgetBelongsToDifferentUser_Returns403Forbidden()
    {
        // Arrange
        const int userId = 1;
        const int budgetId = 1;
        SetupControllerContext(_sut, userId);

        var request = new UpdateBudgetRequest
        {
            AmountLimit = 600.00m
        };

        _budgetServiceMock
            .Setup(x => x.UpdateBudgetAsync(userId, budgetId, request))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _sut.UpdateBudget(userId, budgetId, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
