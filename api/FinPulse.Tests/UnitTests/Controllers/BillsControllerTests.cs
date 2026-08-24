using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class BillsControllerTests : ControllerTestBase
{
    private readonly Mock<IBillService> _billServiceMock;
    private readonly BillsController _sut;

    public BillsControllerTests()
    {
        _billServiceMock = new Mock<IBillService>();
        _sut = new BillsController(_billServiceMock.Object);
    }

    #region GetBills Tests

    [Fact]
    public async Task GetBills_WhenUserOwnsResource_Returns200OkWithBills()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expectedBills = new List<BillResponse>
        {
            new BillResponse { Id = 1, UserId = userId, BillName = "Electric Bill", Category = "Utilities", Amount = 100.00m, PaidDate = null },
            new BillResponse { Id = 2, UserId = userId, BillName = "Rent Payment", Category = "Rent", Amount = 1500.00m, PaidDate = DateTime.UtcNow }
        };

        _billServiceMock
            .Setup(x => x.GetUserBillsAsync(userId, null, null, null, null))
            .ReturnsAsync(expectedBills);

        // Act
        var result = await _sut.GetBills(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedBills);
    }

    [Fact]
    public async Task GetBills_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        // Act
        var result = await _sut.GetBills(userId: 2);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetBills_WithFilters_Returns200OkWithFilteredBills()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        const string category = "Utilities";
        const bool paid = false;

        var expectedBills = new List<BillResponse>
        {
            new BillResponse { Id = 1, UserId = userId, BillName = "Utility Bill", Category = category, Amount = 100.00m, PaidDate = null }
        };

        _billServiceMock
            .Setup(x => x.GetUserBillsAsync(userId, startDate, endDate, category, paid))
            .ReturnsAsync(expectedBills);

        // Act
        var result = await _sut.GetBills(userId, startDate, endDate, category, paid);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedBills);
    }

    [Fact]
    public async Task GetBills_WhenNoBillsExist_Returns200OkWithEmptyList()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _billServiceMock
            .Setup(x => x.GetUserBillsAsync(userId, null, null, null, null))
            .ReturnsAsync(new List<BillResponse>());

        // Act
        var result = await _sut.GetBills(userId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var bills = okResult.Value.Should().BeOfType<List<BillResponse>>().Subject;
        bills.Should().BeEmpty();
    }

    #endregion

    #region CreateBill Tests

    [Fact]
    public async Task CreateBill_WhenUserOwnsResource_Returns201CreatedWithBill()
    {
        // Arrange
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreateBillRequest
        {
            BillName = "Electric Bill",
            Category = "Utilities",
            PaymentMethod = "Bank Transfer",
            CurrencyCode = "USD",
            Amount = 100.00m,
            Description = "Electric bill",
            DueDate = DateTime.UtcNow.AddDays(7),
            PaidDate = null
        };

        var expectedResponse = new BillResponse
        {
            Id = 1,
            UserId = userId,
            BillName = request.BillName,
            Category = request.Category,
            PaymentMethod = request.PaymentMethod,
            CurrencyCode = request.CurrencyCode,
            Amount = request.Amount,
            Description = request.Description,
            DueDate = request.DueDate,
            PaidDate = request.PaidDate
        };

        _billServiceMock
            .Setup(x => x.CreateBillAsync(userId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CreateBill(userId, request);

        // Assert
        var createdResult = result.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateBill_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        var request = new CreateBillRequest
        {
            BillName = "Electric Bill",
            Category = "Utilities",
            PaymentMethod = "Bank Transfer",
            CurrencyCode = "USD",
            Amount = 100.00m,
            DueDate = DateTime.UtcNow.AddDays(7),
            PaidDate = null
        };

        // Act
        var result = await _sut.CreateBill(userId: 2, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateBill Tests

    [Fact]
    public async Task UpdateBill_WhenUserOwnsResourceAndBillExists_Returns200OkWithUpdatedBill()
    {
        // Arrange
        const int userId = 1;
        const int billId = 1;
        SetupControllerContext(_sut, userId);

        var request = new UpdateBillRequest
        {
            Amount = 150.00m,
            PaidDate = DateTime.UtcNow
        };

        var expectedResponse = new BillResponse
        {
            Id = billId,
            UserId = userId,
            BillName = "Electric Bill",
            Category = "Utilities",
            Amount = 150.00m,
            PaidDate = DateTime.UtcNow
        };

        _billServiceMock
            .Setup(x => x.UpdateBillAsync(userId, billId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.UpdateBill(userId, billId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UpdateBill_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        var request = new UpdateBillRequest
        {
            PaidDate = DateTime.UtcNow
        };

        // Act
        var result = await _sut.UpdateBill(userId: 2, billId: 1, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateBill_WhenBillDoesNotExist_Returns404NotFound()
    {
        // Arrange
        const int userId = 1;
        const int billId = 999;
        SetupControllerContext(_sut, userId);

        var request = new UpdateBillRequest
        {
            PaidDate = DateTime.UtcNow
        };

        _billServiceMock
            .Setup(x => x.UpdateBillAsync(userId, billId, request))
            .ReturnsAsync((BillResponse?)null);

        // Act
        var result = await _sut.UpdateBill(userId, billId, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateBill_WhenBillBelongsToDifferentUser_Returns403Forbidden()
    {
        // Arrange
        const int userId = 1;
        const int billId = 1;
        SetupControllerContext(_sut, userId);

        var request = new UpdateBillRequest
        {
            PaidDate = DateTime.UtcNow
        };

        _billServiceMock
            .Setup(x => x.UpdateBillAsync(userId, billId, request))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _sut.UpdateBill(userId, billId, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
