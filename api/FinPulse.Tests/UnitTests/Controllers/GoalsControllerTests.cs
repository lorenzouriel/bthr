using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class GoalsControllerTests : ControllerTestBase
{
    private readonly Mock<IGoalService> _goalServiceMock;
    private readonly GoalsController _sut;

    public GoalsControllerTests()
    {
        _goalServiceMock = new Mock<IGoalService>();
        _sut = new GoalsController(_goalServiceMock.Object);
    }

    #region GetGoals Tests

    [Fact]
    public async Task GetGoals_WhenUserOwnsResource_Returns200OkWithGoals()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expectedGoals = new List<GoalResponse>
        {
            new GoalResponse { Id = 1, UserId = userId, Name = "Emergency Fund", TargetAmount = 10000.00m },
            new GoalResponse { Id = 2, UserId = userId, Name = "Vacation", TargetAmount = 5000.00m }
        };

        _goalServiceMock
            .Setup(x => x.GetUserGoalsAsync(userId, null, null))
            .ReturnsAsync(expectedGoals);

        // Act
        var result = await _sut.GetGoals(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedGoals);
    }

    [Fact]
    public async Task GetGoals_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        // Act
        var result = await _sut.GetGoals(userId: 2);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetGoals_WithFilters_Returns200OkWithFilteredGoals()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        var expectedGoals = new List<GoalResponse>
        {
            new GoalResponse { Id = 1, UserId = userId, Name = "Emergency Fund", TargetAmount = 10000.00m }
        };

        _goalServiceMock
            .Setup(x => x.GetUserGoalsAsync(userId, startDate, endDate))
            .ReturnsAsync(expectedGoals);

        // Act
        var result = await _sut.GetGoals(userId, startDate, endDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedGoals);
    }

    [Fact]
    public async Task GetGoals_WhenNoGoalsExist_Returns200OkWithEmptyList()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _goalServiceMock
            .Setup(x => x.GetUserGoalsAsync(userId, null, null))
            .ReturnsAsync(new List<GoalResponse>());

        // Act
        var result = await _sut.GetGoals(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var goals = okResult.Value.Should().BeOfType<List<GoalResponse>>().Subject;
        goals.Should().BeEmpty();
    }

    #endregion

    #region CreateGoal Tests

    [Fact]
    public async Task CreateGoal_WhenUserOwnsResource_Returns201CreatedWithGoal()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreateGoalRequest
        {
            Name = "Emergency Fund",
            CurrencyCode = "USD",
            TargetAmount = 10000.00m,
            CurrentAmount = 2000.00m,
            Description = "Save for emergencies",
            DueDate = new DateTime(2025, 12, 31)
        };

        var expectedResponse = new GoalResponse
        {
            Id = 1,
            UserId = userId,
            Name = request.Name,
            CurrencyCode = request.CurrencyCode,
            TargetAmount = request.TargetAmount,
            CurrentAmount = request.CurrentAmount,
            Description = request.Description,
            DueDate = request.DueDate
        };

        _goalServiceMock
            .Setup(x => x.CreateGoalAsync(userId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CreateGoal(userId, request);

        // Assert
        var createdResult = result.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateGoal_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        var request = new CreateGoalRequest
        {
            Name = "Emergency Fund",
            CurrencyCode = "USD",
            TargetAmount = 10000.00m,
            CurrentAmount = 2000.00m,
            DueDate = new DateTime(2025, 12, 31)
        };

        // Act
        var result = await _sut.CreateGoal(userId: 2, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateGoal Tests

    [Fact]
    public async Task UpdateGoal_WhenUserOwnsResourceAndGoalExists_Returns200OkWithUpdatedGoal()
    {
        // Arrange
        const int userId = 1;
        const int goalId = 1;
        SetupControllerContext(_sut, userId);

        var request = new UpdateGoalRequest
        {
            CurrentAmount = 3000.00m,
            Description = "Updated goal description"
        };

        var expectedResponse = new GoalResponse
        {
            Id = goalId,
            UserId = userId,
            Name = "Emergency Fund",
            TargetAmount = 10000.00m,
            CurrentAmount = 3000.00m,
            Description = "Updated goal description"
        };

        _goalServiceMock
            .Setup(x => x.UpdateGoalAsync(userId, goalId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.UpdateGoal(userId, goalId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UpdateGoal_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        var request = new UpdateGoalRequest
        {
            CurrentAmount = 3000.00m
        };

        // Act
        var result = await _sut.UpdateGoal(userId: 2, goalId: 1, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateGoal_WhenGoalDoesNotExist_Returns404NotFound()
    {
        // Arrange
        const int userId = 1;
        const int goalId = 999;
        SetupControllerContext(_sut, userId);

        var request = new UpdateGoalRequest
        {
            CurrentAmount = 3000.00m
        };

        _goalServiceMock
            .Setup(x => x.UpdateGoalAsync(userId, goalId, request))
            .ReturnsAsync((GoalResponse?)null);

        // Act
        var result = await _sut.UpdateGoal(userId, goalId, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateGoal_WhenGoalBelongsToDifferentUser_Returns403Forbidden()
    {
        // Arrange
        const int userId = 1;
        const int goalId = 1;
        SetupControllerContext(_sut, userId);

        var request = new UpdateGoalRequest
        {
            CurrentAmount = 3000.00m
        };

        _goalServiceMock
            .Setup(x => x.UpdateGoalAsync(userId, goalId, request))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _sut.UpdateGoal(userId, goalId, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
