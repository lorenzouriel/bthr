using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class InvestmentServiceTests : ServiceTestBase
{
    private readonly InvestmentService _sut;

    public InvestmentServiceTests()
    {
        _sut = new InvestmentService(Context);
    }

    #region CreateInvestmentAsync Tests

    [Fact]
    public async Task CreateInvestmentAsync_WithValidRequest_CreatesInvestmentSuccessfully()
    {
        // Arrange
        var userId = 1;
        var request = new CreateInvestmentRequest
        {
            InvestmentType = "Equity",
            Category = "Stocks",
            AssetName = "Apple Inc.",
            Broker = "Fidelity",
            CurrencyCode = "USD",
            InvestedAmount = 5000.00m,
            CurrentValue = 5500.00m,
            PurchaseDate = DateTime.UtcNow.AddMonths(-6),
            AnnualYieldPercent = 8.5m,
            ProfitLoss = 500.00m
        };

        // Act
        var result = await _sut.CreateInvestmentAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.InvestmentType.Should().Be(request.InvestmentType);
        result.Category.Should().Be(request.Category);
        result.AssetName.Should().Be(request.AssetName);
        result.InvestedAmount.Should().Be(request.InvestedAmount);
        result.CurrentValue.Should().Be(request.CurrentValue);
        result.PurchaseDate.Should().Be(request.PurchaseDate);

        var investment = await Context.Investments.FindAsync(result.Id);
        investment.Should().NotBeNull();
        investment!.Status.Should().Be(1);
    }

    #endregion

    #region GetUserInvestmentsAsync Tests

    [Fact]
    public async Task GetUserInvestmentsAsync_ReturnsOnlyUserInvestments()
    {
        // Arrange
        var userId = 1;
        var otherUserId = 2;

        var userInvestments = new[]
        {
            new InvestmentBuilder().WithUserId(userId).Build(),
            new InvestmentBuilder().WithUserId(userId).Build()
        };
        var otherUserInvestment = new InvestmentBuilder().WithUserId(otherUserId).Build();

        await Context.Investments.AddRangeAsync(userInvestments);
        await Context.Investments.AddAsync(otherUserInvestment);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserInvestmentsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(i => i.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserInvestmentsAsync_FiltersOutDeletedInvestments()
    {
        // Arrange
        var userId = 1;
        var activeInvestment = new InvestmentBuilder().WithUserId(userId).AsActive().Build();
        var deletedInvestment = new InvestmentBuilder().WithUserId(userId).AsDeleted().Build();

        await Context.Investments.AddAsync(activeInvestment);
        await Context.Investments.AddAsync(deletedInvestment);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserInvestmentsAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().NotContain(i => i.Id == deletedInvestment.Id);
    }

    [Fact]
    public async Task GetUserInvestmentsAsync_FiltersByDateRange()
    {
        // Arrange
        var userId = 1;
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        var investmentInRange = new InvestmentBuilder()
            .WithUserId(userId)
            .WithPurchaseDate(new DateTime(2024, 6, 15))
            .Build();
        var investmentBeforeRange = new InvestmentBuilder()
            .WithUserId(userId)
            .WithPurchaseDate(new DateTime(2023, 12, 31))
            .Build();
        var investmentAfterRange = new InvestmentBuilder()
            .WithUserId(userId)
            .WithPurchaseDate(new DateTime(2025, 1, 1))
            .Build();

        await Context.Investments.AddRangeAsync(investmentInRange, investmentBeforeRange, investmentAfterRange);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserInvestmentsAsync(userId, startDate, endDate);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(investmentInRange.Id);
    }

    [Fact]
    public async Task GetUserInvestmentsAsync_FiltersByInvestmentType()
    {
        // Arrange
        var userId = 1;
        var investmentType = "Equity";

        var equityInvestment = new InvestmentBuilder()
            .WithUserId(userId)
            .WithInvestmentType("Equity")
            .Build();
        var bondInvestment = new InvestmentBuilder()
            .WithUserId(userId)
            .WithInvestmentType("Fixed Income")
            .Build();

        await Context.Investments.AddRangeAsync(equityInvestment, bondInvestment);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserInvestmentsAsync(userId, investmentType: investmentType);

        // Assert
        result.Should().HaveCount(1);
        result[0].InvestmentType.Should().Be("Equity");
    }

    [Fact]
    public async Task GetUserInvestmentsAsync_FiltersByCategory()
    {
        // Arrange
        var userId = 1;
        var category = "Stocks";

        var stocksInvestment = new InvestmentBuilder()
            .WithUserId(userId)
            .WithCategory("Stocks")
            .Build();
        var bondsInvestment = new InvestmentBuilder()
            .WithUserId(userId)
            .WithCategory("Bonds")
            .Build();

        await Context.Investments.AddRangeAsync(stocksInvestment, bondsInvestment);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserInvestmentsAsync(userId, category: category);

        // Assert
        result.Should().HaveCount(1);
        result[0].Category.Should().Be("Stocks");
    }

    [Fact]
    public async Task GetUserInvestmentsAsync_FiltersByMultipleCriteria()
    {
        // Arrange
        var userId = 1;
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var investmentType = "Equity";
        var category = "Stocks";

        var matchingInvestment = new InvestmentBuilder()
            .WithUserId(userId)
            .WithInvestmentType("Equity")
            .WithCategory("Stocks")
            .WithPurchaseDate(new DateTime(2024, 6, 15))
            .Build();
        var nonMatchingInvestment = new InvestmentBuilder()
            .WithUserId(userId)
            .WithInvestmentType("Fixed Income")
            .WithCategory("Bonds")
            .WithPurchaseDate(new DateTime(2024, 6, 15))
            .Build();

        await Context.Investments.AddRangeAsync(matchingInvestment, nonMatchingInvestment);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserInvestmentsAsync(userId, startDate, endDate, investmentType, category);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(matchingInvestment.Id);
    }

    [Fact]
    public async Task GetUserInvestmentsAsync_WhenNoInvestments_ReturnsEmptyList()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _sut.GetUserInvestmentsAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateInvestmentAsync Tests

    [Fact]
    public async Task UpdateInvestmentAsync_WithValidRequest_UpdatesInvestmentSuccessfully()
    {
        // Arrange
        var userId = 1;
        var investment = new InvestmentBuilder()
            .WithUserId(userId)
            .WithAssetName("Apple Inc.")
            .WithInvestedAmount(5000.00m)
            .WithCurrentValue(5500.00m)
            .Build();
        await Context.Investments.AddAsync(investment);
        await Context.SaveChangesAsync();

        var request = new UpdateInvestmentRequest
        {
            AssetName = "Apple Inc. (Updated)",
            CurrentValue = 6000.00m,
            AnnualYieldPercent = 10.0m,
            ProfitLoss = 1000.00m
        };

        // Act
        var result = await _sut.UpdateInvestmentAsync(userId, investment.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.AssetName.Should().Be("Apple Inc. (Updated)");
        result.CurrentValue.Should().Be(6000.00m);
        result.AnnualYieldPercent.Should().Be(10.0m);
        result.ProfitLoss.Should().Be(1000.00m);

        var updatedInvestment = await Context.Investments.FindAsync(investment.Id);
        updatedInvestment!.AssetName.Should().Be("Apple Inc. (Updated)");
        updatedInvestment.CurrentValue.Should().Be(6000.00m);
    }

    [Fact]
    public async Task UpdateInvestmentAsync_WithNonExistentInvestment_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateInvestmentRequest
        {
            CurrentValue = 6000.00m
        };

        // Act
        var result = await _sut.UpdateInvestmentAsync(userId, 999, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateInvestmentAsync_WithDeletedInvestment_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var deletedInvestment = new InvestmentBuilder()
            .WithUserId(userId)
            .AsDeleted()
            .Build();
        await Context.Investments.AddAsync(deletedInvestment);
        await Context.SaveChangesAsync();

        var request = new UpdateInvestmentRequest
        {
            CurrentValue = 6000.00m
        };

        // Act
        var result = await _sut.UpdateInvestmentAsync(userId, deletedInvestment.Id, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateInvestmentAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = 1;
        var otherUserId = 2;
        var investment = new InvestmentBuilder()
            .WithUserId(ownerId)
            .Build();
        await Context.Investments.AddAsync(investment);
        await Context.SaveChangesAsync();

        var request = new UpdateInvestmentRequest
        {
            CurrentValue = 6000.00m
        };

        // Act
        var act = async () => await _sut.UpdateInvestmentAsync(otherUserId, investment.Id, request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to update this investment");
    }

    #endregion

    #region DeleteInvestmentAsync Tests

    [Fact]
    public async Task DeleteInvestmentAsync_SoftDeletesInvestment()
    {
        // Arrange
        var userId = 1;
        var investment = new InvestmentBuilder()
            .WithUserId(userId)
            .AsActive()
            .Build();
        await Context.Investments.AddAsync(investment);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteInvestmentAsync(userId, investment.Id);

        // Assert
        result.Should().BeTrue();

        var deletedInvestment = await Context.Investments.FindAsync(investment.Id);
        deletedInvestment!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteInvestmentAsync_WithNonExistentInvestment_ReturnsFalse()
    {
        // Act
        var result = await _sut.DeleteInvestmentAsync(1, 999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteInvestmentAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = 1;
        var otherUserId = 2;
        var investment = new InvestmentBuilder()
            .WithUserId(ownerId)
            .Build();
        await Context.Investments.AddAsync(investment);
        await Context.SaveChangesAsync();

        // Act
        var act = async () => await _sut.DeleteInvestmentAsync(otherUserId, investment.Id);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to delete this investment");
    }

    #endregion
}
