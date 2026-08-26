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
            Name = "Electric Bill",
            Category = "Utilities",
            PaymentMethod = "Bank Transfer",
            Amount = 150.00m,
            CurrencyCode = "USD",
            DueDay = 15,
            RecurrenceType = "Monthly",
            Description = "Monthly electric bill"
        };

        // Act
        var result = await _sut.CreateBillAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.Name.Should().Be(request.Name);
        result.Category.Should().Be(request.Category);
        result.Amount.Should().Be(request.Amount);
        result.DueDay.Should().Be(request.DueDay);
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
    public async Task GetUserBillsAsync_FiltersByYearAndMonth()
    {
        // Arrange
        var userId = 1;

        var billActiveInMonth = new BillBuilder()
            .WithUserId(userId)
            .WithCreatedAt(new DateTime(2024, 1, 1))
            .WithEndDate(null)
            .Build();
        var billCreatedAfterMonth = new BillBuilder()
            .WithUserId(userId)
            .WithCreatedAt(new DateTime(2024, 3, 1))
            .Build();
        var billEndedBeforeMonth = new BillBuilder()
            .WithUserId(userId)
            .WithCreatedAt(new DateTime(2023, 1, 1))
            .WithEndDate(new DateTime(2023, 12, 31))
            .Build();

        await Context.Bills.AddRangeAsync(billActiveInMonth, billCreatedAfterMonth, billEndedBeforeMonth);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserBillsAsync(userId, year: 2024, month: 1);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(billActiveInMonth.Id);
    }

    [Fact]
    public async Task GetUserBillsAsync_ReturnsCorrectCategoryPerBill()
    {
        // Arrange
        var userId = 1;

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
        var result = await _sut.GetUserBillsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(b => b.Id == utilityBill.Id && b.Category == "Utilities");
        result.Should().Contain(b => b.Id == insuranceBill.Id && b.Category == "Insurance");
    }

    [Fact]
    public async Task GetUserBillsAsync_ComputesPaidThisMonth_WhenMatchingExpenseExistsInMonth()
    {
        // Arrange
        var userId = 1;
        var now = DateTime.UtcNow;

        var paidBill = new BillBuilder()
            .WithUserId(userId)
            .WithName("Electric Bill")
            .Build();
        var unpaidBill = new BillBuilder()
            .WithUserId(userId)
            .WithName("Water Bill")
            .Build();

        await Context.Bills.AddRangeAsync(paidBill, unpaidBill);

        var matchingExpense = new ExpenseBuilder()
            .WithUserId(userId)
            .WithDescription("Electric Bill")
            .WithExpenseDate(now)
            .Build();
        await Context.Expenses.AddAsync(matchingExpense);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserBillsAsync(userId, now.Year, now.Month);

        // Assert
        result.Should().HaveCount(2);
        result.Single(b => b.Id == paidBill.Id).PaidThisMonth.Should().BeTrue();
        result.Single(b => b.Id == paidBill.Id).PaidDate.Should().NotBeNull();
        result.Single(b => b.Id == unpaidBill.Id).PaidThisMonth.Should().BeFalse();
        result.Single(b => b.Id == unpaidBill.Id).PaidDate.Should().BeNull();
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
            .WithName("Electric Bill")
            .WithAmount(150.00m)
            .Build();
        await Context.Bills.AddAsync(bill);
        await Context.SaveChangesAsync();

        var request = new UpdateBillRequest
        {
            Name = "Updated Electric Bill",
            Amount = 200.00m,
            Description = "Updated description"
        };

        // Act
        var result = await _sut.UpdateBillAsync(userId, bill.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Electric Bill");
        result.Amount.Should().Be(200.00m);
        result.Description.Should().Be("Updated description");

        var updatedBill = await Context.Bills.FindAsync(bill.Id);
        updatedBill!.Name.Should().Be("Updated Electric Bill");
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
