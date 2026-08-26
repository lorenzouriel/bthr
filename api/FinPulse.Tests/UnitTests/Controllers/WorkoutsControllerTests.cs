using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class WorkoutsControllerTests : ControllerTestBase
{
    private readonly Mock<IWorkoutService> _serviceMock;
    private readonly WorkoutsController _sut;

    public WorkoutsControllerTests()
    {
        _serviceMock = new Mock<IWorkoutService>();
        _sut = new WorkoutsController(_serviceMock.Object);
    }

    #region GetWorkouts Tests

    [Fact]
    public async Task GetWorkouts_WhenUserOwnsResource_Returns200OkWithWorkouts()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expected = new List<WorkoutResponse>
        {
            new WorkoutResponse { Id = 1, UserId = userId, RoutineName = "Push Day" }
        };

        _serviceMock.Setup(x => x.GetUserWorkoutsAsync(userId, null, null)).ReturnsAsync(expected);

        var result = await _sut.GetWorkouts(userId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetWorkouts_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.GetWorkouts(userId: 2);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetWorkouts_WithFilters_Returns200OkWithFilteredWorkouts()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var startDate = new DateTime(2026, 1, 1);
        var endDate = new DateTime(2026, 12, 31);
        var expected = new List<WorkoutResponse> { new WorkoutResponse { Id = 1, UserId = userId } };

        _serviceMock.Setup(x => x.GetUserWorkoutsAsync(userId, startDate, endDate)).ReturnsAsync(expected);

        var result = await _sut.GetWorkouts(userId, startDate, endDate);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    #endregion

    #region CreateWorkout Tests

    [Fact]
    public async Task CreateWorkout_WhenUserOwnsResource_Returns201Created()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreateWorkoutRequest { WorkoutDate = DateTime.UtcNow, RoutineName = "Push Day" };
        var expected = new WorkoutResponse { Id = 1, UserId = userId, RoutineName = "Push Day" };

        _serviceMock.Setup(x => x.CreateWorkoutAsync(userId, request)).ReturnsAsync(expected);

        var result = await _sut.CreateWorkout(userId, request);

        var objResult = result.Should().BeOfType<ObjectResult>().Subject;
        objResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateWorkout_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.CreateWorkout(userId: 2, new CreateWorkoutRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateWorkout Tests

    [Fact]
    public async Task UpdateWorkout_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.UpdateWorkoutAsync(userId, 999, It.IsAny<UpdateWorkoutRequest>()))
            .ReturnsAsync((WorkoutResponse?)null);

        var result = await _sut.UpdateWorkout(userId, 999, new UpdateWorkoutRequest());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateWorkout_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.UpdateWorkout(userId: 2, workoutId: 1, new UpdateWorkoutRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region DeleteWorkout Tests

    [Fact]
    public async Task DeleteWorkout_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.DeleteWorkoutAsync(userId, 999)).ReturnsAsync(false);

        var result = await _sut.DeleteWorkout(userId, 999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteWorkout_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.DeleteWorkout(userId: 2, workoutId: 1);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
