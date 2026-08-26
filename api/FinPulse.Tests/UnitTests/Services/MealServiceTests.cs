using FluentAssertions;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class MealServiceTests : ServiceTestBase
{
    private readonly MealService _sut;

    public MealServiceTests()
    {
        _sut = new MealService(Context);
    }

    #region CreateMealAsync Tests

    [Fact]
    public async Task CreateMealAsync_WithValidRequest_CreatesSuccessfully()
    {
        var userId = 1;
        var request = new CreateMealRequest
        {
            MealDate = DateTime.UtcNow.Date,
            MealType = "Breakfast",
            Calories = 450m,
            ProteinGrams = 25m
        };

        var result = await _sut.CreateMealAsync(userId, request);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.MealType.Should().Be("Breakfast");

        var meal = await Context.Meals.FindAsync(result.Id);
        meal!.Status.Should().Be(1);
    }

    #endregion

    #region GetUserMealsAsync Tests

    [Fact]
    public async Task GetUserMealsAsync_ReturnsOnlyUserMeals()
    {
        var userId = 1;
        var otherUserId = 2;

        await Context.Meals.AddRangeAsync(
            new MealBuilder().WithUserId(userId).Build(),
            new MealBuilder().WithUserId(userId).Build());
        await Context.Meals.AddAsync(new MealBuilder().WithUserId(otherUserId).Build());
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserMealsAsync(userId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(m => m.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetUserMealsAsync_FiltersOutDeletedMeals()
    {
        var userId = 1;
        var active = new MealBuilder().WithUserId(userId).AsActive().Build();
        var deleted = new MealBuilder().WithUserId(userId).AsDeleted().Build();

        await Context.Meals.AddAsync(active);
        await Context.Meals.AddAsync(deleted);
        await Context.SaveChangesAsync();

        var result = await _sut.GetUserMealsAsync(userId);

        result.Should().HaveCount(1);
        result.Should().NotContain(m => m.Id == deleted.Id);
    }

    #endregion

    #region UpdateMealAsync Tests

    [Fact]
    public async Task UpdateMealAsync_WithValidRequest_UpdatesSuccessfully()
    {
        var userId = 1;
        var meal = new MealBuilder().WithUserId(userId).Build();
        await Context.Meals.AddAsync(meal);
        await Context.SaveChangesAsync();

        var request = new UpdateMealRequest { Calories = 700m };
        var result = await _sut.UpdateMealAsync(userId, meal.Id, request);

        result.Should().NotBeNull();
        result!.Calories.Should().Be(700m);
    }

    [Fact]
    public async Task UpdateMealAsync_WithNonExistentMeal_ReturnsNull()
    {
        var result = await _sut.UpdateMealAsync(1, 999, new UpdateMealRequest());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateMealAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var meal = new MealBuilder().WithUserId(ownerId).Build();
        await Context.Meals.AddAsync(meal);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.UpdateMealAsync(otherUserId, meal.Id, new UpdateMealRequest());

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to update this meal");
    }

    #endregion

    #region DeleteMealAsync Tests

    [Fact]
    public async Task DeleteMealAsync_SoftDeletesMeal()
    {
        var userId = 1;
        var meal = new MealBuilder().WithUserId(userId).AsActive().Build();
        await Context.Meals.AddAsync(meal);
        await Context.SaveChangesAsync();

        var result = await _sut.DeleteMealAsync(userId, meal.Id);

        result.Should().BeTrue();
        var deleted = await Context.Meals.FindAsync(meal.Id);
        deleted!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteMealAsync_WithWrongUserId_ThrowsUnauthorizedAccessException()
    {
        var ownerId = 1;
        var otherUserId = 2;
        var meal = new MealBuilder().WithUserId(ownerId).Build();
        await Context.Meals.AddAsync(meal);
        await Context.SaveChangesAsync();

        var act = async () => await _sut.DeleteMealAsync(otherUserId, meal.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Not authorized to delete this meal");
    }

    #endregion
}
