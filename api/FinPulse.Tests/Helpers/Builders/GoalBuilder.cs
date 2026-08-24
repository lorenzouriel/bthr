using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

/// <summary>
/// Fluent builder for creating Goal test data with realistic fake data.
/// </summary>
public class GoalBuilder
{
    private readonly Goal _goal;
    private static readonly Faker _faker = new Faker();

    public GoalBuilder()
    {
        _goal = new Goal
        {
            Id = 0, // Let EF Core auto-generate
            UserId = _faker.Random.Int(1, 100),
            Name = _faker.PickRandom(new[] { "Emergency Fund", "Vacation", "New Car", "House Down Payment", "Retirement" }),
            TargetAmount = _faker.Finance.Amount(1000, 50000),
            CurrentAmount = _faker.Finance.Amount(0, 1000),
            CurrencyCode = "USD",
            DueDate = _faker.Date.Future(2),
            CreatedAt = DateTime.UtcNow,
            Status = 1 // Active
        };
    }

    public GoalBuilder WithId(int id)
    {
        _goal.Id = id;
        return this;
    }

    public GoalBuilder WithUserId(int userId)
    {
        _goal.UserId = userId;
        return this;
    }

    public GoalBuilder WithName(string name)
    {
        _goal.Name = name;
        return this;
    }

    public GoalBuilder WithTargetAmount(decimal targetAmount)
    {
        _goal.TargetAmount = targetAmount;
        return this;
    }

    public GoalBuilder WithCurrentAmount(decimal currentAmount)
    {
        _goal.CurrentAmount = currentAmount;
        return this;
    }

    public GoalBuilder WithCurrencyCode(string currencyCode)
    {
        _goal.CurrencyCode = currencyCode;
        return this;
    }

    public GoalBuilder WithDueDate(DateTime dueDate)
    {
        _goal.DueDate = dueDate;
        return this;
    }

    public GoalBuilder WithCreatedAt(DateTime createdAt)
    {
        _goal.CreatedAt = createdAt;
        return this;
    }

    public GoalBuilder WithStatus(short status)
    {
        _goal.Status = status;
        return this;
    }

    public GoalBuilder AsActive()
    {
        _goal.Status = 1;
        return this;
    }

    public GoalBuilder AsDeleted()
    {
        _goal.Status = 0;
        return this;
    }

    public Goal Build() => _goal;
}
