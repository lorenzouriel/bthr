using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

public class MealBuilder
{
    private readonly Meal _meal;
    private static readonly Faker _faker = new Faker();

    public MealBuilder()
    {
        _meal = new Meal
        {
            Id = 0,
            UserId = _faker.Random.Int(1, 100),
            MealDate = _faker.Date.Recent(14),
            MealType = _faker.PickRandom(new[] { "Breakfast", "Lunch", "Dinner", "Snack" }),
            Description = _faker.Lorem.Sentence(),
            Calories = _faker.Random.Decimal(100, 1200),
            ProteinGrams = _faker.Random.Decimal(5, 60),
            CarbsGrams = _faker.Random.Decimal(5, 120),
            FatGrams = _faker.Random.Decimal(5, 60),
            CreatedAt = DateTime.UtcNow,
            Status = 1
        };
    }

    public MealBuilder WithId(int id) { _meal.Id = id; return this; }
    public MealBuilder WithUserId(int userId) { _meal.UserId = userId; return this; }
    public MealBuilder WithMealDate(DateTime date) { _meal.MealDate = date; return this; }
    public MealBuilder AsActive() { _meal.Status = 1; return this; }
    public MealBuilder AsDeleted() { _meal.Status = 0; return this; }
    public Meal Build() => _meal;
}
