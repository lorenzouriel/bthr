using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class EarningServiceTests : ServiceTestBase
{
    private readonly EarningService _sut;

    public EarningServiceTests()
    {
        _sut = new EarningService(Context);
    }

    #region CreateEarningAsync Tests

    [Fact]
    public async Task CreateEarningAsync_WithValidRequest_CreatesEarningSuccessfully()
    {
        // Arrange
        var userId = 1;
        var request = new CreateEarningRequest
        {
            Category = "Salary",
            PaymentMethod = "Bank Transfer",
            CurrencyCode = "USD",
            Amount = 5000.00m,
            Description = "Monthly salary",
            EarningDate = DateTime.UtcNow.Date
        };

        // Act
        var result = await _sut.CreateEarningAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.Category.Should().Be(request.Category);
        result.PaymentMethod.Should().Be(request.PaymentMethod);
        result.Amount.Should().Be(request.Amount);
        result.Description.Should().Be(request.Description);
        result.EarningDate.Should().Be(request.EarningDate);

        var earning = await Context.Earnings.FindAsync(result.Id);
        earning.Should().NotBeNull();
        earning!.Status.Should().Be(1);
    }

    #endregion

    #region GetUserEarningsAsync Tests

    [Fact]
    public async Task GetUserEarningsAsync_ReturnsOnlyUserEarnings()
    {
        // Arrange
        var userId = 1;
        var otherUserId = 2;

        var userEarnings = new[]
        {
            new EarningBuilder().WithUserId(userId).Build(),
            new EarningBuilder().WithUserId(userId).Build()
        };
        var otherUserEarning = new EarningBuilder().WithUserId(otherUserId).Build();

        await Context.Earnings.AddRangeAsync(userEarnings);
        await Context.Earnings.AddAsync(otherUserEarning);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserEarningsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserEarningsAsync_FiltersOutDeletedEarnings()
    {
        // Arrange
        var userId = 1;
        var activeEarning = new EarningBuilder().WithUserId(userId).AsActive().Build();
        var deletedEarning = new EarningBuilder().WithUserId(userId).AsDeleted().Build();

        await Context.Earnings.AddAsync(activeEarning);
        await Context.Earnings.AddAsync(deletedEarning);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserEarningsAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().NotContain(e => e.Id == deletedEarning.Id);
    }

    [Fact]
    public async Task GetUserEarningsAsync_FiltersByDateRange()
    {
        // Arrange
        var userId = 1;
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        var earningInRange = new EarningBuilder()
            .WithUserId(userId)
            .WithEarningDate(new DateTime(2024, 1, 15))
            .Build();
        var earningBeforeRange = new EarningBuilder()
            .WithUserId(userId)
            .WithEarningDate(new DateTime(2023, 12, 31))
            .Build();
        var earningAfterRange = new EarningBuilder()
            .WithUserId(userId)
            .WithEarningDate(new DateTime(2024, 2, 1))
            .Build();

        await Context.Earnings.AddRangeAsync(earningInRange, earningBeforeRange, earningAfterRange);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserEarningsAsync(userId, startDate, endDate);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(earningInRange.Id);
    }

    [Fact]
    public async Task GetUserEarningsAsync_FiltersByCategory()
    {
        // Arrange
        var userId = 1;
        var category = "Salary";

        var salaryEarning = new EarningBuilder()
            .WithUserId(userId)
            .WithCategory("Salary")
            .Build();
        var freelanceEarning = new EarningBuilder()
            .WithUserId(userId)
            .WithCategory("Freelance")
            .Build();

        await Context.Earnings.AddRangeAsync(salaryEarning, freelanceEarning);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserEarningsAsync(userId, category: category);

        // Assert
        result.Should().HaveCount(1);
        result[0].Category.Should().Be("Salary");
    }

    [Fact]
    public async Task GetUserEarningsAsync_WhenNoEarnings_ReturnsEmptyList()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _sut.GetUserEarningsAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateEarningAsync Tests

    [Fact]
    public async Task UpdateEarningAsync_WithValidRequest_UpdatesEarningSuccessfully()
    {
        // Arrange
        var userId = 1;
        var earning = new EarningBuilder()
            .WithUserId(userId)
            .WithCategory("Salary")
            .WithAmount(5000.00m)
            .Build();
        await Context.Earnings.AddAsync(earning);
        await Context.SaveChangesAsync();

        var request = new UpdateEarningRequest
        {
            Category = "Bonus",
            Amount = 6000.00m,
            Description = "Updated description"
        };

        // Act
        var result = await _sut.UpdateEarningAsync(userId, earning.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Category.Should().Be("Bonus");
        result.Amount.Should().Be(6000.00m);
        result.Description.Should().Be("Updated description");

        var updatedEarning = await Context.Earnings.FindAsync(earning.Id);
        updatedEarning!.Category.Should().Be("Bonus");
        updatedEarning.Amount.Should().Be(6000.00m);
    }

    [Fact]
    public async Task UpdateEarningAsync_WithNonExistentEarning_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateEarningRequest
        {
            Amount = 1000.00m
        };

        // Act
        var result = await _sut.UpdateEarningAsync(userId, 999, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateEarningAsync_WithDeletedEarning_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var deletedEarning = new EarningBuilder()
            .WithUserId(userId)
            .AsDeleted()
            .Build();
        await Context.Earnings.AddAsync(deletedEarning);
        await Context.SaveChangesAsync();

        var request = new UpdateEarningRequest
        {
            Amount = 1000.00m
        };

        // Act
        var result = await _sut.UpdateEarningAsync(userId, deletedEarning.Id, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateEarningAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = 1;
        var otherUserId = 2;
        var earning = new EarningBuilder()
            .WithUserId(ownerId)
            .Build();
        await Context.Earnings.AddAsync(earning);
        await Context.SaveChangesAsync();

        var request = new UpdateEarningRequest
        {
            Amount = 1000.00m
        };

        // Act
        var act = async () => await _sut.UpdateEarningAsync(otherUserId, earning.Id, request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to update this earning");
    }

    #endregion

    #region DeleteEarningAsync Tests

    [Fact]
    public async Task DeleteEarningAsync_SoftDeletesEarning()
    {
        // Arrange
        var userId = 1;
        var earning = new EarningBuilder()
            .WithUserId(userId)
            .AsActive()
            .Build();
        await Context.Earnings.AddAsync(earning);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteEarningAsync(userId, earning.Id);

        // Assert
        result.Should().BeTrue();

        var deletedEarning = await Context.Earnings.FindAsync(earning.Id);
        deletedEarning!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteEarningAsync_WithNonExistentEarning_ReturnsFalse()
    {
        // Act
        var result = await _sut.DeleteEarningAsync(1, 999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteEarningAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = 1;
        var otherUserId = 2;
        var earning = new EarningBuilder()
            .WithUserId(ownerId)
            .Build();
        await Context.Earnings.AddAsync(earning);
        await Context.SaveChangesAsync();

        // Act
        var act = async () => await _sut.DeleteEarningAsync(otherUserId, earning.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to delete this earning");
    }

    #endregion
}
