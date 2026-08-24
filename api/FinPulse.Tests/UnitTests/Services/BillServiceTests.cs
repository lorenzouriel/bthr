using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class BillServiceTests : ServiceTestBase
{
    private readonly BillService _sut;

    public BillServiceTests()
    {
        _sut = new BillService(Context);
    }

    #region CreateBillAsync Tests

    [Fact]
    public async Task CreateBillAsync_WithValidRequest_CreatesBillSuccessfully()
    {
        // Arrange
        var userId = 1;
        var request = new CreateBillRequest
        {
            BillName = "Electric Bill",
            Category = "Utilities",
            PaymentMethod = "Bank Transfer",
            Amount = 150.00m,
            CurrencyCode = "USD",
            DueDate = DateTime.UtcNow.AddDays(15),
            RecurrenceType = "Monthly",
            RecurrenceInterval = 1,
            Description = "Monthly electric bill"
        };

        // Act
        var result = await _sut.CreateBillAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.BillName.Should().Be(request.BillName);
        result.Category.Should().Be(request.Category);
        result.Amount.Should().Be(request.Amount);
        result.DueDate.Should().Be(request.DueDate);
        result.RecurrenceType.Should().Be(request.RecurrenceType);

        var bill = await Context.Bills.FindAsync(result.Id);
        bill.Should().NotBeNull();
        bill!.Status.Should().Be(1);
    }

    #endregion

    #region GetUserBillsAsync Tests

    [Fact]
    public async Task GetUserBillsAsync_ReturnsOnlyUserBills()
    {
        // Arrange
        var userId = 1;
        var otherUserId = 2;

        var userBills = new[]
        {
            new BillBuilder().WithUserId(userId).Build(),
            new BillBuilder().WithUserId(userId).Build()
        };
        var otherUserBill = new BillBuilder().WithUserId(otherUserId).Build();

        await Context.Bills.AddRangeAsync(userBills);
        await Context.Bills.AddAsync(otherUserBill);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserBillsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(b => b.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserBillsAsync_FiltersOutDeletedBills()
    {
        // Arrange
        var userId = 1;
        var activeBill = new BillBuilder().WithUserId(userId).AsActive().Build();
        var deletedBill = new BillBuilder().WithUserId(userId).AsDeleted().Build();

        await Context.Bills.AddAsync(activeBill);
        await Context.Bills.AddAsync(deletedBill);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserBillsAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().NotContain(b => b.Id == deletedBill.Id);
    }

    [Fact]
    public async Task GetUserBillsAsync_FiltersByDateRange()
    {
        // Arrange
        var userId = 1;
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        var billInRange = new BillBuilder()
            .WithUserId(userId)
            .WithDueDate(new DateTime(2024, 1, 15))
            .Build();
        var billBeforeRange = new BillBuilder()
            .WithUserId(userId)
            .WithDueDate(new DateTime(2023, 12, 31))
            .Build();
        var billAfterRange = new BillBuilder()
            .WithUserId(userId)
            .WithDueDate(new DateTime(2024, 2, 1))
            .Build();

        await Context.Bills.AddRangeAsync(billInRange, billBeforeRange, billAfterRange);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserBillsAsync(userId, startDate, endDate);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(billInRange.Id);
    }

    [Fact]
    public async Task GetUserBillsAsync_FiltersByCategory()
    {
        // Arrange
        var userId = 1;
        var category = "Utilities";

        var utilityBill = new BillBuilder()
            .WithUserId(userId)
            .WithCategory("Utilities")
            .Build();
        var insuranceBill = new BillBuilder()
            .WithUserId(userId)
            .WithCategory("Insurance")
            .Build();

        await Context.Bills.AddRangeAsync(utilityBill, insuranceBill);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserBillsAsync(userId, category: category);

        // Assert
        result.Should().HaveCount(1);
        result[0].Category.Should().Be("Utilities");
    }

    [Fact]
    public async Task GetUserBillsAsync_FiltersByPaidStatus_ReturnsOnlyPaidBills()
    {
        // Arrange
        var userId = 1;

        var paidBill = new BillBuilder()
            .WithUserId(userId)
            .AsPaid(DateTime.UtcNow)
            .Build();
        var unpaidBill = new BillBuilder()
            .WithUserId(userId)
            .AsUnpaid()
            .Build();

        await Context.Bills.AddRangeAsync(paidBill, unpaidBill);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserBillsAsync(userId, paid: true);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(paidBill.Id);
        result[0].PaidDate.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserBillsAsync_FiltersByPaidStatus_ReturnsOnlyUnpaidBills()
    {
        // Arrange
        var userId = 1;

        var paidBill = new BillBuilder()
            .WithUserId(userId)
            .AsPaid(DateTime.UtcNow)
            .Build();
        var unpaidBill = new BillBuilder()
            .WithUserId(userId)
            .AsUnpaid()
            .Build();

        await Context.Bills.AddRangeAsync(paidBill, unpaidBill);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserBillsAsync(userId, paid: false);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(unpaidBill.Id);
        result[0].PaidDate.Should().BeNull();
    }

    [Fact]
    public async Task GetUserBillsAsync_WhenNoBills_ReturnsEmptyList()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _sut.GetUserBillsAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateBillAsync Tests

    [Fact]
    public async Task UpdateBillAsync_WithValidRequest_UpdatesBillSuccessfully()
    {
        // Arrange
        var userId = 1;
        var bill = new BillBuilder()
            .WithUserId(userId)
            .WithBillName("Electric Bill")
            .WithAmount(150.00m)
            .Build();
        await Context.Bills.AddAsync(bill);
        await Context.SaveChangesAsync();

        var request = new UpdateBillRequest
        {
            BillName = "Updated Electric Bill",
            Amount = 200.00m,
            Description = "Updated description",
            PaidDate = DateTime.UtcNow
        };

        // Act
        var result = await _sut.UpdateBillAsync(userId, bill.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.BillName.Should().Be("Updated Electric Bill");
        result.Amount.Should().Be(200.00m);
        result.Description.Should().Be("Updated description");
        result.PaidDate.Should().NotBeNull();

        var updatedBill = await Context.Bills.FindAsync(bill.Id);
        updatedBill!.BillName.Should().Be("Updated Electric Bill");
        updatedBill.Amount.Should().Be(200.00m);
    }

    [Fact]
    public async Task UpdateBillAsync_WithNonExistentBill_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateBillRequest
        {
            Amount = 100.00m
        };

        // Act
        var result = await _sut.UpdateBillAsync(userId, 999, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateBillAsync_WithDeletedBill_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var deletedBill = new BillBuilder()
            .WithUserId(userId)
            .AsDeleted()
            .Build();
        await Context.Bills.AddAsync(deletedBill);
        await Context.SaveChangesAsync();

        var request = new UpdateBillRequest
        {
            Amount = 100.00m
        };

        // Act
        var result = await _sut.UpdateBillAsync(userId, deletedBill.Id, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateBillAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = 1;
        var otherUserId = 2;
        var bill = new BillBuilder()
            .WithUserId(ownerId)
            .Build();
        await Context.Bills.AddAsync(bill);
        await Context.SaveChangesAsync();

        var request = new UpdateBillRequest
        {
            Amount = 100.00m
        };

        // Act
        var act = async () => await _sut.UpdateBillAsync(otherUserId, bill.Id, request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to update this bill");
    }

    #endregion

    #region DeleteBillAsync Tests

    [Fact]
    public async Task DeleteBillAsync_SoftDeletesBill()
    {
        // Arrange
        var userId = 1;
        var bill = new BillBuilder()
            .WithUserId(userId)
            .AsActive()
            .Build();
        await Context.Bills.AddAsync(bill);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteBillAsync(userId, bill.Id);

        // Assert
        result.Should().BeTrue();

        var deletedBill = await Context.Bills.FindAsync(bill.Id);
        deletedBill!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteBillAsync_WithNonExistentBill_ReturnsFalse()
    {
        // Act
        var result = await _sut.DeleteBillAsync(1, 999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBillAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = 1;
        var otherUserId = 2;
        var bill = new BillBuilder()
            .WithUserId(ownerId)
            .Build();
        await Context.Bills.AddAsync(bill);
        await Context.SaveChangesAsync();

        // Act
        var act = async () => await _sut.DeleteBillAsync(otherUserId, bill.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to delete this bill");
    }

    #endregion
}
