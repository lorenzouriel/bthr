using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

/// <summary>
/// Fluent builder for creating Earning test data with realistic fake data.
/// </summary>
public class EarningBuilder
{
    private readonly Earning _earning;
    private static readonly Faker _faker = new Faker();

    public EarningBuilder()
    {
        _earning = new Earning
        {
            Id = 0, // Let EF Core auto-generate
            UserId = _faker.Random.Int(1, 100),
            Category = _faker.PickRandom(new[] { "Salary", "Freelance", "Investment", "Bonus", "Other" }),
            PaymentMethod = _faker.PickRandom(new[] { "Bank Transfer", "Cash", "Check", "PayPal" }),
            Amount = _faker.Finance.Amount(500, 5000),
            CurrencyCode = "USD",
            Description = _faker.Lorem.Sentence(),
            EarningDate = _faker.Date.Recent(30),
            CreatedAt = DateTime.UtcNow,
            Status = 1 // Active
        };
    }

    public EarningBuilder WithId(int id)
    {
        _earning.Id = id;
        return this;
    }

    public EarningBuilder WithUserId(int userId)
    {
        _earning.UserId = userId;
        return this;
    }

    public EarningBuilder WithCategory(string category)
    {
        _earning.Category = category;
        return this;
    }

    public EarningBuilder WithPaymentMethod(string paymentMethod)
    {
        _earning.PaymentMethod = paymentMethod;
        return this;
    }

    public EarningBuilder WithAmount(decimal amount)
    {
        _earning.Amount = amount;
        return this;
    }

    public EarningBuilder WithCurrencyCode(string currencyCode)
    {
        _earning.CurrencyCode = currencyCode;
        return this;
    }

    public EarningBuilder WithDescription(string description)
    {
        _earning.Description = description;
        return this;
    }

    public EarningBuilder WithEarningDate(DateTime date)
    {
        _earning.EarningDate = date;
        return this;
    }

    public EarningBuilder WithCreatedAt(DateTime createdAt)
    {
        _earning.CreatedAt = createdAt;
        return this;
    }

    public EarningBuilder WithStatus(byte status)
    {
        _earning.Status = status;
        return this;
    }

    public EarningBuilder AsActive()
    {
        _earning.Status = 1;
        return this;
    }

    public EarningBuilder AsDeleted()
    {
        _earning.Status = 0;
        return this;
    }

    public Earning Build() => _earning;
}
