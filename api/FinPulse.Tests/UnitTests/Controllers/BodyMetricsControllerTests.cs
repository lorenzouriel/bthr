using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class BodyMetricsControllerTests : ControllerTestBase
{
    private readonly Mock<IBodyMetricService> _serviceMock;
    private readonly BodyMetricsController _sut;

    public BodyMetricsControllerTests()
    {
        _serviceMock = new Mock<IBodyMetricService>();
        _sut = new BodyMetricsController(_serviceMock.Object);
    }

    #region GetBodyMetrics Tests

    [Fact]
    public async Task GetBodyMetrics_WhenUserOwnsResource_Returns200OkWithRecords()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expected = new List<BodyMetricResponse> { new BodyMetricResponse { Id = 1, UserId = userId, WeightKg = 75m } };

        _serviceMock.Setup(x => x.GetUserBodyMetricsAsync(userId, null, null)).ReturnsAsync(expected);

        var result = await _sut.GetBodyMetrics(userId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetBodyMetrics_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.GetBodyMetrics(userId: 2);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region CreateBodyMetric Tests

    [Fact]
    public async Task CreateBodyMetric_WhenUserOwnsResource_Returns201Created()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreateBodyMetricRequest { MeasuredDate = DateTime.UtcNow, WeightKg = 75m };
        var expected = new BodyMetricResponse { Id = 1, UserId = userId, WeightKg = 75m };

        _serviceMock.Setup(x => x.CreateBodyMetricAsync(userId, request)).ReturnsAsync(expected);

        var result = await _sut.CreateBodyMetric(userId, request);

        var objResult = result.Should().BeOfType<ObjectResult>().Subject;
        objResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateBodyMetric_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.CreateBodyMetric(userId: 2, new CreateBodyMetricRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateBodyMetric Tests

    [Fact]
    public async Task UpdateBodyMetric_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.UpdateBodyMetricAsync(userId, 999, It.IsAny<UpdateBodyMetricRequest>()))
            .ReturnsAsync((BodyMetricResponse?)null);

        var result = await _sut.UpdateBodyMetric(userId, 999, new UpdateBodyMetricRequest());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateBodyMetric_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.UpdateBodyMetric(userId: 2, bodyMetricId: 1, new UpdateBodyMetricRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region DeleteBodyMetric Tests

    [Fact]
    public async Task DeleteBodyMetric_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.DeleteBodyMetricAsync(userId, 999)).ReturnsAsync(false);

        var result = await _sut.DeleteBodyMetric(userId, 999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteBodyMetric_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.DeleteBodyMetric(userId: 2, bodyMetricId: 1);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
