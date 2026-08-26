using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class WaterIntakeControllerTests : ControllerTestBase
{
    private readonly Mock<IWaterIntakeService> _serviceMock;
    private readonly WaterIntakeController _sut;

    public WaterIntakeControllerTests()
    {
        _serviceMock = new Mock<IWaterIntakeService>();
        _sut = new WaterIntakeController(_serviceMock.Object);
    }

    #region GetWaterIntake Tests

    [Fact]
    public async Task GetWaterIntake_WhenUserOwnsResource_Returns200OkWithRecords()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expected = new List<WaterIntakeResponse> { new WaterIntakeResponse { Id = 1, UserId = userId, AmountMl = 500 } };

        _serviceMock.Setup(x => x.GetUserWaterIntakeAsync(userId, null, null)).ReturnsAsync(expected);

        var result = await _sut.GetWaterIntake(userId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetWaterIntake_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.GetWaterIntake(userId: 2);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region CreateWaterIntake Tests

    [Fact]
    public async Task CreateWaterIntake_WhenUserOwnsResource_Returns201Created()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreateWaterIntakeRequest { IntakeDate = DateTime.UtcNow, AmountMl = 500 };
        var expected = new WaterIntakeResponse { Id = 1, UserId = userId, AmountMl = 500 };

        _serviceMock.Setup(x => x.CreateWaterIntakeAsync(userId, request)).ReturnsAsync(expected);

        var result = await _sut.CreateWaterIntake(userId, request);

        var objResult = result.Should().BeOfType<ObjectResult>().Subject;
        objResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateWaterIntake_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.CreateWaterIntake(userId: 2, new CreateWaterIntakeRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateWaterIntake Tests

    [Fact]
    public async Task UpdateWaterIntake_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.UpdateWaterIntakeAsync(userId, 999, It.IsAny<UpdateWaterIntakeRequest>()))
            .ReturnsAsync((WaterIntakeResponse?)null);

        var result = await _sut.UpdateWaterIntake(userId, 999, new UpdateWaterIntakeRequest());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateWaterIntake_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.UpdateWaterIntake(userId: 2, waterIntakeId: 1, new UpdateWaterIntakeRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region DeleteWaterIntake Tests

    [Fact]
    public async Task DeleteWaterIntake_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.DeleteWaterIntakeAsync(userId, 999)).ReturnsAsync(false);

        var result = await _sut.DeleteWaterIntake(userId, 999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteWaterIntake_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.DeleteWaterIntake(userId: 2, waterIntakeId: 1);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
