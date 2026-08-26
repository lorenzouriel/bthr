using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class WeeklyRoutinesControllerTests : ControllerTestBase
{
    private readonly Mock<IWeeklyRoutineService> _serviceMock;
    private readonly WeeklyRoutinesController _sut;

    public WeeklyRoutinesControllerTests()
    {
        _serviceMock = new Mock<IWeeklyRoutineService>();
        _sut = new WeeklyRoutinesController(_serviceMock.Object);
    }

    #region GetWeeklyRoutines Tests

    [Fact]
    public async Task GetWeeklyRoutines_WhenUserOwnsResource_Returns200OkWithRoutines()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expected = new List<WeeklyRoutineResponse>
        {
            new WeeklyRoutineResponse { Id = 1, UserId = userId, DayOfWeek = 1, RoutineName = "Push Day" }
        };

        _serviceMock.Setup(x => x.GetUserWeeklyRoutinesAsync(userId)).ReturnsAsync(expected);

        var result = await _sut.GetWeeklyRoutines(userId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetWeeklyRoutines_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.GetWeeklyRoutines(userId: 2);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region CreateWeeklyRoutine Tests

    [Fact]
    public async Task CreateWeeklyRoutine_WhenUserOwnsResource_Returns201Created()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreateWeeklyRoutineRequest { DayOfWeek = 1, RoutineName = "Push Day" };
        var expected = new WeeklyRoutineResponse { Id = 1, UserId = userId, DayOfWeek = 1, RoutineName = "Push Day" };

        _serviceMock.Setup(x => x.CreateWeeklyRoutineAsync(userId, request)).ReturnsAsync(expected);

        var result = await _sut.CreateWeeklyRoutine(userId, request);

        var objResult = result.Should().BeOfType<ObjectResult>().Subject;
        objResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateWeeklyRoutine_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.CreateWeeklyRoutine(userId: 2, new CreateWeeklyRoutineRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateWeeklyRoutine Tests

    [Fact]
    public async Task UpdateWeeklyRoutine_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.UpdateWeeklyRoutineAsync(userId, 999, It.IsAny<UpdateWeeklyRoutineRequest>()))
            .ReturnsAsync((WeeklyRoutineResponse?)null);

        var result = await _sut.UpdateWeeklyRoutine(userId, 999, new UpdateWeeklyRoutineRequest());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateWeeklyRoutine_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.UpdateWeeklyRoutine(userId: 2, routineId: 1, new UpdateWeeklyRoutineRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region DeleteWeeklyRoutine Tests

    [Fact]
    public async Task DeleteWeeklyRoutine_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.DeleteWeeklyRoutineAsync(userId, 999)).ReturnsAsync(false);

        var result = await _sut.DeleteWeeklyRoutine(userId, 999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteWeeklyRoutine_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.DeleteWeeklyRoutine(userId: 2, routineId: 1);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
