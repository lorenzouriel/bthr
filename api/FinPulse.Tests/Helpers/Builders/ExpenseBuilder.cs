using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

/// <summary>
/// Fluent builder for creating Expense test data with realistic fake data.
/// </summary>
public class ExpenseBuilder
{
    private readonly Expense _expense;
    private static readonly Faker _faker = new Faker();

    public ExpenseBuilder()
    {
        _expense = new Expense
        {
            Id = 0, // Let EF Core auto-generate
            UserId = _faker.Random.Int(1, 100),
            Category = _faker.PickRandom(new[] { "Food", "Transport", "Entertainment", "Utilities", "Healthcare", "Shopping" }),
            PaymentMethod = _faker.PickRandom(new[] { "Cash", "Credit Card", "Debit Card", "Bank Transfer" }),
            Amount = _faker.Finance.Amount(10, 500),
            CurrencyCode = "USD",
            Description = _faker.Lorem.Sentence(),
            ExpenseDate = _faker.Date.Recent(30),
            CreatedAt = DateTime.UtcNow,
            Status = 1 // Active
        };
    }

    public ExpenseBuilder WithId(int id)
    {
        _expense.Id = id;
        return this;
    }

    public ExpenseBuilder WithUserId(int userId)
    {
        _expense.UserId = userId;
        return this;
    }

    public ExpenseBuilder WithCategory(string category)
    {
        _expense.Category = category;
        return this;
    }

    public ExpenseBuilder WithPaymentMethod(string paymentMethod)
    {
        _expense.PaymentMethod = paymentMethod;
        return this;
    }

    public ExpenseBuilder WithAmount(decimal amount)
    {
        _expense.Amount = amount;
        return this;
    }

    public ExpenseBuilder WithCurrencyCode(string currencyCode)
    {
        _expense.CurrencyCode = currencyCode;
        return this;
    }

    public ExpenseBuilder WithDescription(string description)
    {
        _expense.Description = description;
        return this;
    }

    public ExpenseBuilder WithExpenseDate(DateTime date)
    {
        _expense.ExpenseDate = date;
        return this;
    }

    public ExpenseBuilder WithCreatedAt(DateTime createdAt)
    {
        _expense.CreatedAt = createdAt;
        return this;
    }

    public ExpenseBuilder WithStatus(byte status)
    {
        _expense.Status = status;
        return this;
    }

    public ExpenseBuilder AsActive()
    {
        _expense.Status = 1;
        return this;
    }

    public ExpenseBuilder AsDeleted()
    {
        _expense.Status = 0;
        return this;
    }

    public Expense Build() => _expense;
}
