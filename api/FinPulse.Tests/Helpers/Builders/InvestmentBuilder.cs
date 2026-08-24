using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

/// <summary>
/// Fluent builder for creating Investment test data with realistic fake data.
/// </summary>
public class InvestmentBuilder
{
    private readonly Investment _investment;
    private static readonly Faker _faker = new Faker();

    public InvestmentBuilder()
    {
        _investment = new Investment
        {
            Id = 0, // Let EF Core auto-generate
            UserId = _faker.Random.Int(1, 100),
            InvestmentType = _faker.PickRandom(new[] { "Equity", "Fixed Income", "Commodity", "Real Estate", "Alternative" }),
            Category = _faker.PickRandom(new[] { "Stocks", "Bonds", "Mutual Funds", "Real Estate", "Cryptocurrency" }),
            AssetName = _faker.Company.CompanyName(),
            Broker = _faker.Company.CompanyName(),
            InvestedAmount = _faker.Finance.Amount(1000, 10000),
            CurrentValue = _faker.Finance.Amount(1000, 15000),
            CurrencyCode = "USD",
            PurchaseDate = _faker.Date.Past(2),
            CreatedAt = DateTime.UtcNow,
            Status = 1 // Active
        };
    }

    public InvestmentBuilder WithId(int id)
    {
        _investment.Id = id;
        return this;
    }

    public InvestmentBuilder WithUserId(int userId)
    {
        _investment.UserId = userId;
        return this;
    }

    public InvestmentBuilder WithInvestmentType(string investmentType)
    {
        _investment.InvestmentType = investmentType;
        return this;
    }

    public InvestmentBuilder WithCategory(string category)
    {
        _investment.Category = category;
        return this;
    }

    public InvestmentBuilder WithAssetName(string assetName)
    {
        _investment.AssetName = assetName;
        return this;
    }

    public InvestmentBuilder WithInvestedAmount(decimal investedAmount)
    {
        _investment.InvestedAmount = investedAmount;
        return this;
    }

    public InvestmentBuilder WithCurrentValue(decimal? currentValue)
    {
        _investment.CurrentValue = currentValue;
        return this;
    }

    public InvestmentBuilder WithCurrencyCode(string currencyCode)
    {
        _investment.CurrencyCode = currencyCode;
        return this;
    }

    public InvestmentBuilder WithPurchaseDate(DateTime purchaseDate)
    {
        _investment.PurchaseDate = purchaseDate;
        return this;
    }

    public InvestmentBuilder WithCreatedAt(DateTime createdAt)
    {
        _investment.CreatedAt = createdAt;
        return this;
    }

    public InvestmentBuilder WithStatus(short status)
    {
        _investment.Status = status;
        return this;
    }

    public InvestmentBuilder AsActive()
    {
        _investment.Status = 1;
        return this;
    }

    public InvestmentBuilder AsDeleted()
    {
        _investment.Status = 0;
        return this;
    }

    public Investment Build() => _investment;
}
