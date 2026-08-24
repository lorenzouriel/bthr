using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

/// <summary>
/// Fluent builder for creating Budget test data with realistic fake data.
/// </summary>
public class BudgetBuilder
{
    private readonly Budget _budget;
    private static readonly Faker _faker = new Faker();

    public BudgetBuilder()
    {
        var startDate = _faker.Date.Recent(10);
        _budget = new Budget
        {
            Id = 0, // Let EF Core auto-generate
            UserId = _faker.Random.Int(1, 100),
            Name = _faker.PickRandom(new[] { "Monthly Food Budget", "Transport Budget", "Entertainment Budget", "Utilities Budget" }),
            AmountLimit = _faker.Finance.Amount(500, 2000),
            CurrencyCode = "USD",
            StartDate = startDate,
            EndDate = startDate.AddMonths(1),
            CreatedAt = DateTime.UtcNow,
            Status = 1 // Active
        };
    }

    public BudgetBuilder WithId(int id)
    {
        _budget.Id = id;
        return this;
    }

    public BudgetBuilder WithUserId(int userId)
    {
        _budget.UserId = userId;
        return this;
    }

    public BudgetBuilder WithName(string name)
    {
        _budget.Name = name;
        return this;
    }

    public BudgetBuilder WithAmountLimit(decimal amountLimit)
    {
        _budget.AmountLimit = amountLimit;
        return this;
    }

    public BudgetBuilder WithCurrencyCode(string currencyCode)
    {
        _budget.CurrencyCode = currencyCode;
        return this;
    }

    public BudgetBuilder WithDateRange(DateTime startDate, DateTime endDate)
    {
        _budget.StartDate = startDate;
        _budget.EndDate = endDate;
        return this;
    }

    public BudgetBuilder WithStartDate(DateTime startDate)
    {
        _budget.StartDate = startDate;
        return this;
    }

    public BudgetBuilder WithEndDate(DateTime endDate)
    {
        _budget.EndDate = endDate;
        return this;
    }

    public BudgetBuilder WithCreatedAt(DateTime createdAt)
    {
        _budget.CreatedAt = createdAt;
        return this;
    }

    public BudgetBuilder WithStatus(short status)
    {
        _budget.Status = status;
        return this;
    }

    public BudgetBuilder AsActive()
    {
        _budget.Status = 1;
        return this;
    }

    public BudgetBuilder AsDeleted()
    {
        _budget.Status = 0;
        return this;
    }

    public Budget Build() => _budget;
}
