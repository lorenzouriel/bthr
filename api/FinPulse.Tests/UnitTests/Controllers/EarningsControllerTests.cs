using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class EarningsControllerTests : ControllerTestBase
{
    private readonly Mock<IEarningService> _earningServiceMock;
    private readonly EarningsController _sut;

    public EarningsControllerTests()
    {
        _earningServiceMock = new Mock<IEarningService>();
        _sut = new EarningsController(_earningServiceMock.Object);
    }

    #region GetEarnings Tests

    [Fact]
    public async Task GetEarnings_WhenUserOwnsResource_Returns200OkWithEarnings()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expectedEarnings = new List<EarningResponse>
        {
            new EarningResponse { Id = 1, UserId = userId, Category = "Salary", Amount = 5000.00m },
            new EarningResponse { Id = 2, UserId = userId, Category = "Freelance", Amount = 1000.00m }
        };

        _earningServiceMock
            .Setup(x => x.GetUserEarningsAsync(userId, null, null, null))
            .ReturnsAsync(expectedEarnings);

        // Act
        var result = await _sut.GetEarnings(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedEarnings);
    }

    [Fact]
    public async Task GetEarnings_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        // Act
        var result = await _sut.GetEarnings(userId: 2);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetEarnings_WithFilters_Returns200OkWithFilteredEarnings()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        const string category = "Salary";

        var expectedEarnings = new List<EarningResponse>
        {
            new EarningResponse { Id = 1, UserId = userId, Category = category, Amount = 5000.00m }
        };

        _earningServiceMock
            .Setup(x => x.GetUserEarningsAsync(userId, startDate, endDate, category))
            .ReturnsAsync(expectedEarnings);

        // Act
        var result = await _sut.GetEarnings(userId, startDate, endDate, category);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedEarnings);
    }

    [Fact]
    public async Task GetEarnings_WhenNoEarningsExist_Returns200OkWithEmptyList()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _earningServiceMock
            .Setup(x => x.GetUserEarningsAsync(userId, null, null, null))
            .ReturnsAsync(new List<EarningResponse>());

        // Act
        var result = await _sut.GetEarnings(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var earnings = okResult.Value.Should().BeOfType<List<EarningResponse>>().Subject;
        earnings.Should().BeEmpty();
    }

    #endregion

    #region CreateEarning Tests

    [Fact]
    public async Task CreateEarning_WhenUserOwnsResource_Returns201CreatedWithEarning()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreateEarningRequest
        {
            Category = "Salary",
            PaymentMethod = "Bank Transfer",
            CurrencyCode = "USD",
            Amount = 5000.00m,
            Description = "Monthly salary",
            EarningDate = DateTime.UtcNow
        };

        var expectedResponse = new EarningResponse
        {
            Id = 1,
            UserId = userId,
            Category = request.Category,
            PaymentMethod = request.PaymentMethod,
            CurrencyCode = request.CurrencyCode,
            Amount = request.Amount,
            Description = request.Description,
            EarningDate = request.EarningDate
        };

        _earningServiceMock
            .Setup(x => x.CreateEarningAsync(userId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CreateEarning(userId, request);

        // Assert
        var createdResult = result.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateEarning_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        var request = new CreateEarningRequest
        {
            Category = "Salary",
            PaymentMethod = "Bank Transfer",
            CurrencyCode = "USD",
            Amount = 5000.00m,
            EarningDate = DateTime.UtcNow
        };

        // Act
        var result = await _sut.CreateEarning(userId: 2, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateEarning Tests

    [Fact]
    public async Task UpdateEarning_WhenUserOwnsResourceAndEarningExists_Returns200OkWithUpdatedEarning()
    {
        // Arrange
        const int userId = 1;
        const int earningId = 1;
        SetupControllerContext(_sut, userId);

        var request = new UpdateEarningRequest
        {
            Category = "Bonus",
            Amount = 2000.00m
        };

        var expectedResponse = new EarningResponse
        {
            Id = earningId,
            UserId = userId,
            Category = "Bonus",
            Amount = 2000.00m
        };

        _earningServiceMock
            .Setup(x => x.UpdateEarningAsync(userId, earningId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.UpdateEarning(userId, earningId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UpdateEarning_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        var request = new UpdateEarningRequest
        {
            Amount = 2000.00m
        };

        // Act
        var result = await _sut.UpdateEarning(userId: 2, earningId: 1, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateEarning_WhenEarningDoesNotExist_Returns404NotFound()
    {
        // Arrange
        const int userId = 1;
        const int earningId = 999;
        SetupControllerContext(_sut, userId);

        var request = new UpdateEarningRequest
        {
            Amount = 2000.00m
        };

        _earningServiceMock
            .Setup(x => x.UpdateEarningAsync(userId, earningId, request))
            .ReturnsAsync((EarningResponse?)null);

        // Act
        var result = await _sut.UpdateEarning(userId, earningId, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateEarning_WhenEarningBelongsToDifferentUser_Returns403Forbidden()
    {
        // Arrange
        const int userId = 1;
        const int earningId = 1;
        SetupControllerContext(_sut, userId);

        var request = new UpdateEarningRequest
        {
            Amount = 2000.00m
        };

        _earningServiceMock
            .Setup(x => x.UpdateEarningAsync(userId, earningId, request))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _sut.UpdateEarning(userId, earningId, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
