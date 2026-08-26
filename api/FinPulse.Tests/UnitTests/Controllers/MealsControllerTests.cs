using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class MealsControllerTests : ControllerTestBase
{
    private readonly Mock<IMealService> _serviceMock;
    private readonly MealsController _sut;

    public MealsControllerTests()
    {
        _serviceMock = new Mock<IMealService>();
        _sut = new MealsController(_serviceMock.Object);
    }

    #region GetMeals Tests

    [Fact]
    public async Task GetMeals_WhenUserOwnsResource_Returns200OkWithMeals()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var expected = new List<MealResponse> { new MealResponse { Id = 1, UserId = userId, MealType = "Breakfast" } };

        _serviceMock.Setup(x => x.GetUserMealsAsync(userId, null, null)).ReturnsAsync(expected);

        var result = await _sut.GetMeals(userId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetMeals_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.GetMeals(userId: 2);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region CreateMeal Tests

    [Fact]
    public async Task CreateMeal_WhenUserOwnsResource_Returns201Created()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        var request = new CreateMealRequest { MealDate = DateTime.UtcNow, MealType = "Breakfast", Calories = 400m };
        var expected = new MealResponse { Id = 1, UserId = userId, MealType = "Breakfast" };

        _serviceMock.Setup(x => x.CreateMealAsync(userId, request)).ReturnsAsync(expected);

        var result = await _sut.CreateMeal(userId, request);

        var objResult = result.Should().BeOfType<ObjectResult>().Subject;
        objResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateMeal_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.CreateMeal(userId: 2, new CreateMealRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateMeal Tests

    [Fact]
    public async Task UpdateMeal_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.UpdateMealAsync(userId, 999, It.IsAny<UpdateMealRequest>()))
            .ReturnsAsync((MealResponse?)null);

        var result = await _sut.UpdateMeal(userId, 999, new UpdateMealRequest());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateMeal_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.UpdateMeal(userId: 2, mealId: 1, new UpdateMealRequest());

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region DeleteMeal Tests

    [Fact]
    public async Task DeleteMeal_WhenNotFound_Returns404NotFound()
    {
        const int userId = 1;
        SetupControllerContext(_sut, userId);

        _serviceMock.Setup(x => x.DeleteMealAsync(userId, 999)).ReturnsAsync(false);

        var result = await _sut.DeleteMeal(userId, 999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteMeal_WhenUserDoesNotOwnResource_Returns403Forbidden()
    {
        SetupControllerContext(_sut, userId: 1);

        var result = await _sut.DeleteMeal(userId: 2, mealId: 1);

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion
}
