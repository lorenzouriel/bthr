using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

/// <summary>
/// Fluent builder for creating Bill test data with realistic fake data.
/// </summary>
public class BillBuilder
{
    private readonly Bill _bill;
    private static readonly Faker _faker = new Faker();

    public BillBuilder()
    {
        _bill = new Bill
        {
            Id = 0, // Let EF Core auto-generate
            UserId = _faker.Random.Int(1, 100),
            Name = _faker.PickRandom(new[] { "Electric Bill", "Water Bill", "Internet Bill", "Rent", "Insurance Premium" }),
            Category = _faker.PickRandom(new[] { "Utilities", "Rent", "Insurance", "Subscription", "Loan" }),
            Amount = _faker.Finance.Amount(50, 1000),
            CurrencyCode = "USD",
            Description = _faker.Lorem.Sentence(),
            DueDay = (byte)_faker.Random.Int(1, 28),
            IsRecurrent = true,
            RecurrenceType = _faker.PickRandom(new string?[] { null, "Monthly", "Quarterly", "Yearly" }),
            CreatedAt = DateTime.UtcNow,
            Status = 1 // Active
        };
    }

    public BillBuilder WithId(int id)
    {
        _bill.Id = id;
        return this;
    }

    public BillBuilder WithUserId(int userId)
    {
        _bill.UserId = userId;
        return this;
    }

    public BillBuilder WithName(string name)
    {
        _bill.Name = name;
        return this;
    }

    public BillBuilder WithCategory(string category)
    {
        _bill.Category = category;
        return this;
    }

    public BillBuilder WithAmount(decimal amount)
    {
        _bill.Amount = amount;
        return this;
    }

    public BillBuilder WithCurrencyCode(string currencyCode)
    {
        _bill.CurrencyCode = currencyCode;
        return this;
    }

    public BillBuilder WithDescription(string? description)
    {
        _bill.Description = description;
        return this;
    }

    public BillBuilder WithDueDay(byte dueDay)
    {
        _bill.DueDay = dueDay;
        return this;
    }

    public BillBuilder WithEndDate(DateTime? endDate)
    {
        _bill.EndDate = endDate;
        return this;
    }

    public BillBuilder AsRecurring(string recurrenceType = "Monthly")
    {
        _bill.RecurrenceType = recurrenceType;
        return this;
    }

    public BillBuilder AsOneTime()
    {
        _bill.RecurrenceType = null;
        return this;
    }

    public BillBuilder WithCreatedAt(DateTime createdAt)
    {
        _bill.CreatedAt = createdAt;
        return this;
    }

    public BillBuilder WithStatus(short status)
    {
        _bill.Status = status;
        return this;
    }

    public BillBuilder AsActive()
    {
        _bill.Status = 1;
        return this;
    }

    public BillBuilder AsDeleted()
    {
        _bill.Status = 0;
        return this;
    }

    public Bill Build() => _bill;
}
