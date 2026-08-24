using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IGoalService
{
    Task<List<GoalResponse>> GetUserGoalsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<GoalResponse> CreateGoalAsync(int userId, CreateGoalRequest request);
    Task<GoalResponse?> UpdateGoalAsync(int userId, int goalId, UpdateGoalRequest request);
    Task<bool> DeleteGoalAsync(int userId, int goalId);
}

public class GoalService : IGoalService
{
    private readonly ApplicationDbContext _context;

    public GoalService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<GoalResponse>> GetUserGoalsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Goals
            .Where(g => g.UserId == userId && g.Status != 0);

        if (startDate.HasValue)
            query = query.Where(g => g.DueDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(g => g.DueDate <= endDate.Value);

        return await query
            .OrderByDescending(g => g.DueDate)
            .Select(g => new GoalResponse
            {
                Id = g.Id,
                UserId = g.UserId,
                Name = g.Name,
                Description = g.Description,
                TargetAmount = g.TargetAmount,
                CurrentAmount = g.CurrentAmount,
                CurrencyCode = g.CurrencyCode,
                DueDate = g.DueDate,
                Status = g.Status,
                CreatedAt = g.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<GoalResponse> CreateGoalAsync(int userId, CreateGoalRequest request)
    {
        var goal = new Goal
        {
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            TargetAmount = request.TargetAmount,
            CurrentAmount = request.CurrentAmount,
            CurrencyCode = request.CurrencyCode,
            DueDate = request.DueDate,
            Status = 1
        };

        _context.Goals.Add(goal);
        await _context.SaveChangesAsync();

        return new GoalResponse
        {
            Id = goal.Id,
            UserId = goal.UserId,
            Name = goal.Name,
            Description = goal.Description,
            TargetAmount = goal.TargetAmount,
            CurrentAmount = goal.CurrentAmount,
            CurrencyCode = goal.CurrencyCode,
            DueDate = goal.DueDate,
            Status = goal.Status,
            CreatedAt = goal.CreatedAt
        };
    }

    public async Task<GoalResponse?> UpdateGoalAsync(int userId, int goalId, UpdateGoalRequest request)
    {
        var goal = await _context.Goals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.Status != 0);

        if (goal == null)
            return null;

        if (goal.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this goal");

        if (request.Name != null) goal.Name = request.Name;
        if (request.Description != null) goal.Description = request.Description;
        if (request.TargetAmount.HasValue) goal.TargetAmount = request.TargetAmount.Value;
        if (request.CurrentAmount.HasValue) goal.CurrentAmount = request.CurrentAmount.Value;
        if (request.CurrencyCode != null) goal.CurrencyCode = request.CurrencyCode;
        if (request.DueDate.HasValue) goal.DueDate = request.DueDate.Value;
        if (request.Status.HasValue) goal.Status = request.Status.Value;

        await _context.SaveChangesAsync();

        return new GoalResponse
        {
            Id = goal.Id,
            UserId = goal.UserId,
            Name = goal.Name,
            Description = goal.Description,
            TargetAmount = goal.TargetAmount,
            CurrentAmount = goal.CurrentAmount,
            CurrencyCode = goal.CurrencyCode,
            DueDate = goal.DueDate,
            Status = goal.Status,
            CreatedAt = goal.CreatedAt
        };
    }

    public async Task<bool> DeleteGoalAsync(int userId, int goalId)
    {
        var goal = await _context.Goals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.Status != 0);

        if (goal == null)
            return false;

        if (goal.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this goal");

        goal.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
