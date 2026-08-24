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
            BillName = _faker.PickRandom(new[] { "Electric Bill", "Water Bill", "Internet Bill", "Rent", "Insurance Premium" }),
            Category = _faker.PickRandom(new[] { "Utilities", "Rent", "Insurance", "Subscription", "Loan" }),
            Amount = _faker.Finance.Amount(50, 1000),
            CurrencyCode = "USD",
            Description = _faker.Lorem.Sentence(),
            DueDate = _faker.Date.Future(1),
            PaidDate = null,
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

    public BillBuilder WithBillName(string billName)
    {
        _bill.BillName = billName;
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

    public BillBuilder WithDueDate(DateTime dueDate)
    {
        _bill.DueDate = dueDate;
        return this;
    }

    public BillBuilder AsPaid(DateTime? paidDate = null)
    {
        _bill.PaidDate = paidDate ?? DateTime.UtcNow;
        return this;
    }

    public BillBuilder AsUnpaid()
    {
        _bill.PaidDate = null;
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
