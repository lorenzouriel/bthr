using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class InvestmentsControllerTests : ControllerTestBase
{
    private readonly Mock<IInvestmentService> _investmentServiceMock;
    private readonly InvestmentsController _sut;

    public InvestmentsControllerTests()
    {
        _investmentServiceMock = new Mock<IInvestmentService>();
        _sut = new InvestmentsController(_investmentServiceMock.Object);
    }

    #region GetInvestments Tests

    [Fact]
    public async Task GetInvestments_WhenUserOwnsResource_Returns200OkWithInvestments()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expectedInvestments = new List<InvestmentResponse>
        {
            new InvestmentResponse { Id = 1, UserId = userId, InvestmentType = "Stocks", Category = "Tech", AssetName = "AAPL", InvestedAmount = 1000.00m },
            new InvestmentResponse { Id = 2, UserId = userId, InvestmentType = "Bonds", Category = "Government", AssetName = "US Treasury", InvestedAmount = 500.00m }
        };

        _investmentServiceMock
            .Setup(x => x.GetUserInvestmentsAsync(userId, null, null, null, null))
            .ReturnsAsync(expectedInvestments);

        // Act
        var result = await _sut.GetInvestments(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedInvestments);
    }

    [Fact]
    public async Task GetInvestments_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        // Act
        var result = await _sut.GetInvestments(userId: 2);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetInvestments_WithFilters_Returns200OkWithFilteredInvestments()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        const string investmentType = "Stocks";
        const string category = "Tech";

        var expectedInvestments = new List<InvestmentResponse>
        {
            new InvestmentResponse { Id = 1, UserId = userId, InvestmentType = investmentType, Category = category, AssetName = "AAPL", InvestedAmount = 1000.00m }
        };

        _investmentServiceMock
            .Setup(x => x.GetUserInvestmentsAsync(userId, startDate, endDate, investmentType, category))
            .ReturnsAsync(expectedInvestments);

        // Act
        var result = await _sut.GetInvestments(userId, startDate, endDate, investmentType, category);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedInvestments);
    }

    [Fact]
    public async Task GetInvestments_WhenNoInvestmentsExist_Returns200OkWithEmptyList()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _investmentServiceMock
            .Setup(x => x.GetUserInvestmentsAsync(userId, null, null, null, null))
            .ReturnsAsync(new List<InvestmentResponse>());

        // Act
        var result = await _sut.GetInvestments(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var investments = okResult.Value.Should().BeOfType<List<InvestmentResponse>>().Subject;
        investments.Should().BeEmpty();
    }

    #endregion

    #region CreateInvestment Tests

    [Fact]
    public async Task CreateInvestment_WhenUserOwnsResource_Returns201CreatedWithInvestment()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreateInvestmentRequest
        {
            InvestmentType = "Stocks",
            Category = "Tech",
            AssetName = "Apple Inc.",
            CurrencyCode = "USD",
            InvestedAmount = 1000.00m,
            PurchaseDate = DateTime.UtcNow
        };

        var expectedResponse = new InvestmentResponse
        {
            Id = 1,
            UserId = userId,
            InvestmentType = request.InvestmentType,
            Category = request.Category,
            AssetName = request.AssetName,
            CurrencyCode = request.CurrencyCode,
            InvestedAmount = request.InvestedAmount,
            PurchaseDate = request.PurchaseDate
        };

        _investmentServiceMock
            .Setup(x => x.CreateInvestmentAsync(userId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CreateInvestment(userId, request);

        // Assert
        var createdResult = result.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateInvestment_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        var request = new CreateInvestmentRequest
        {
            InvestmentType = "Stocks",
            Category = "Tech",
            AssetName = "Apple Inc.",
            CurrencyCode = "USD",
            InvestedAmount = 1000.00m,
            PurchaseDate = DateTime.UtcNow
        };

        // Act
        var result = await _sut.CreateInvestment(userId: 2, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateInvestment Tests

    [Fact]
    public async Task UpdateInvestment_WhenUserOwnsResourceAndInvestmentExists_Returns200OkWithUpdatedInvestment()
    {
        // Arrange
        const int userId = 1;
        const int investmentId = 1;
        SetupControllerContext(_sut, userId);

        var request = new UpdateInvestmentRequest
        {
            InvestedAmount = 1500.00m,
            AssetName = "Updated investment"
        };

        var expectedResponse = new InvestmentResponse
        {
            Id = investmentId,
            UserId = userId,
            InvestmentType = "Stocks",
            Category = "Tech",
            AssetName = "Updated investment",
            InvestedAmount = 1500.00m
        };

        _investmentServiceMock
            .Setup(x => x.UpdateInvestmentAsync(userId, investmentId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.UpdateInvestment(userId, investmentId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UpdateInvestment_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        var request = new UpdateInvestmentRequest
        {
            InvestedAmount = 1500.00m
        };

        // Act
        var result = await _sut.UpdateInvestment(userId: 2, investmentId: 1, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateInvestment_WhenInvestmentDoesNotExist_Returns404NotFound()
    {
        // Arrange
        const int userId = 1;
        const int investmentId = 999;
        SetupControllerContext(_sut, userId);

        var request = new UpdateInvestmentRequest
        {
            InvestedAmount = 1500.00m
        };

        _investmentServiceMock
            .Setup(x => x.UpdateInvestmentAsync(userId, investmentId, request))
            .ReturnsAsync((InvestmentResponse?)null);

        // Act
        var result = await _sut.UpdateInvestment(userId, investmentId, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateInvestment_WhenInvestmentBelongsToDifferentUser_Returns403Forbidden()
    {
        // Arrange
        const int userId = 1;
        const int investmentId = 1;
        SetupControllerContext(_sut, userId);

        var request = new UpdateInvestmentRequest
        {
            InvestedAmount = 1500.00m
        };

        _investmentServiceMock
            .Setup(x => x.UpdateInvestmentAsync(userId, investmentId, request))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _sut.UpdateInvestment(userId, investmentId, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
