using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IMealService
{
    Task<List<MealResponse>> GetUserMealsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<MealResponse> CreateMealAsync(int userId, CreateMealRequest request);
    Task<MealResponse?> UpdateMealAsync(int userId, int mealId, UpdateMealRequest request);
    Task<bool> DeleteMealAsync(int userId, int mealId);
}

public class MealService : IMealService
{
    private readonly ApplicationDbContext _context;

    public MealService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MealResponse>> GetUserMealsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Meals.Where(m => m.UserId == userId && m.Status != 0);

        if (startDate.HasValue)
            query = query.Where(m => m.MealDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(m => m.MealDate <= endDate.Value);

        return await query
            .OrderByDescending(m => m.MealDate)
            .Select(m => new MealResponse
            {
                Id = m.Id,
                UserId = m.UserId,
                MealDate = m.MealDate,
                MealType = m.MealType,
                Description = m.Description,
                Calories = m.Calories,
                ProteinGrams = m.ProteinGrams,
                CarbsGrams = m.CarbsGrams,
                FatGrams = m.FatGrams,
                Status = m.Status,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<MealResponse> CreateMealAsync(int userId, CreateMealRequest request)
    {
        var meal = new Meal
        {
            UserId = userId,
            MealDate = request.MealDate,
            MealType = request.MealType,
            Description = request.Description,
            Calories = request.Calories,
            ProteinGrams = request.ProteinGrams,
            CarbsGrams = request.CarbsGrams,
            FatGrams = request.FatGrams,
            Status = 1
        };

        _context.Meals.Add(meal);
        await _context.SaveChangesAsync();

        return new MealResponse
        {
            Id = meal.Id,
            UserId = meal.UserId,
            MealDate = meal.MealDate,
            MealType = meal.MealType,
            Description = meal.Description,
            Calories = meal.Calories,
            ProteinGrams = meal.ProteinGrams,
            CarbsGrams = meal.CarbsGrams,
            FatGrams = meal.FatGrams,
            Status = meal.Status,
            CreatedAt = meal.CreatedAt
        };
    }

    public async Task<MealResponse?> UpdateMealAsync(int userId, int mealId, UpdateMealRequest request)
    {
        var meal = await _context.Meals.FirstOrDefaultAsync(m => m.Id == mealId && m.Status != 0);

        if (meal == null)
            return null;

        if (meal.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this meal");

        if (request.MealDate.HasValue) meal.MealDate = request.MealDate.Value;
        if (request.MealType != null) meal.MealType = request.MealType;
        if (request.Description != null) meal.Description = request.Description;
        if (request.Calories.HasValue) meal.Calories = request.Calories.Value;
        if (request.ProteinGrams.HasValue) meal.ProteinGrams = request.ProteinGrams.Value;
        if (request.CarbsGrams.HasValue) meal.CarbsGrams = request.CarbsGrams.Value;
        if (request.FatGrams.HasValue) meal.FatGrams = request.FatGrams.Value;
        if (request.Status.HasValue) meal.Status = request.Status.Value;

        await _context.SaveChangesAsync();

        return new MealResponse
        {
            Id = meal.Id,
            UserId = meal.UserId,
            MealDate = meal.MealDate,
            MealType = meal.MealType,
            Description = meal.Description,
            Calories = meal.Calories,
            ProteinGrams = meal.ProteinGrams,
            CarbsGrams = meal.CarbsGrams,
            FatGrams = meal.FatGrams,
            Status = meal.Status,
            CreatedAt = meal.CreatedAt
        };
    }

    public async Task<bool> DeleteMealAsync(int userId, int mealId)
    {
        var meal = await _context.Meals.FirstOrDefaultAsync(m => m.Id == mealId && m.Status != 0);

        if (meal == null)
            return false;

        if (meal.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this meal");

        meal.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
