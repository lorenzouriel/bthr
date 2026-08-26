using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class SleepLogsControllerTests : ControllerTestBase
{
    private readonly Mock<ISleepLogService> _serviceMock;
    private readonly SleepLogsController _sut;

    public SleepLogsControllerTests()
    {
        _serviceMock = new Mock<ISleepLogService>();
        _sut = new SleepLogsController(_serviceMock.Object);
    }

    #region GetSleepLogs Tests

    [Fact]
    public async Task GetSleepLogs_WhenUserOwnsResource_Returns200OkWithLogs()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expected = new List<SleepLogResponse> { new SleepLogResponse { Id = 1, UserId = userId, TotalHours = 7.5m } };

        _serviceMock.Setup(x => x.GetUserSleepLogsAsync(userId, null, null)).ReturnsAsync(expected);

        var result = await _sut.GetSleepLogs(userId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetSleepLogs_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.GetSleepLogs(userId: 2);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region CreateSleepLog Tests

    [Fact]
    public async Task CreateSleepLog_WhenUserOwnsResource_Returns201Created()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreateSleepLogRequest
        {
            BedTime = new DateTime(2026, 8, 24, 23, 0, 0),
            WakeTime = new DateTime(2026, 8, 25, 6, 30, 0)
        };
        var expected = new SleepLogResponse { Id = 1, UserId = userId, TotalHours = 7.5m };

        _serviceMock.Setup(x => x.CreateSleepLogAsync(userId, request)).ReturnsAsync(expected);

        var result = await _sut.CreateSleepLog(userId, request);

        var objResult = result.Should().BeOfType<ObjectResult>().Subject;
        objResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateSleepLog_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.CreateSleepLog(userId: 2, new CreateSleepLogRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateSleepLog Tests

    [Fact]
    public async Task UpdateSleepLog_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.UpdateSleepLogAsync(userId, 999, It.IsAny<UpdateSleepLogRequest>()))
            .ReturnsAsync((SleepLogResponse?)null);

        var result = await _sut.UpdateSleepLog(userId, 999, new UpdateSleepLogRequest());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateSleepLog_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.UpdateSleepLog(userId: 2, sleepLogId: 1, new UpdateSleepLogRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region DeleteSleepLog Tests

    [Fact]
    public async Task DeleteSleepLog_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.DeleteSleepLogAsync(userId, 999)).ReturnsAsync(false);

        var result = await _sut.DeleteSleepLog(userId, 999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteSleepLog_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.DeleteSleepLog(userId: 2, sleepLogId: 1);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
