using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IBudgetService
{
    Task<List<BudgetResponse>> GetUserBudgetsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<BudgetResponse> CreateBudgetAsync(int userId, CreateBudgetRequest request);
    Task<BudgetResponse?> UpdateBudgetAsync(int userId, int budgetId, UpdateBudgetRequest request);
    Task<bool> DeleteBudgetAsync(int userId, int budgetId);
}

public class BudgetService : IBudgetService
{
    private readonly ApplicationDbContext _context;

    public BudgetService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BudgetResponse>> GetUserBudgetsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Budgets
            .Where(b => b.UserId == userId && b.Status != 0);

        if (startDate.HasValue)
            query = query.Where(b => b.StartDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(b => b.EndDate <= endDate.Value);

        return await query
            .OrderByDescending(b => b.StartDate)
            .Select(b => new BudgetResponse
            {
                Id = b.Id,
                UserId = b.UserId,
                Name = b.Name,
                Description = b.Description,
                AmountLimit = b.AmountLimit,
                CurrencyCode = b.CurrencyCode,
                StartDate = b.StartDate,
                EndDate = b.EndDate
            })
            .ToListAsync();
    }

    public async Task<BudgetResponse> CreateBudgetAsync(int userId, CreateBudgetRequest request)
    {
        var budget = new Budget
        {
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            AmountLimit = request.AmountLimit,
            CurrencyCode = request.CurrencyCode,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = 1
        };

        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        return new BudgetResponse
        {
            Id = budget.Id,
            UserId = budget.UserId,
            Name = budget.Name,
            Description = budget.Description,
            AmountLimit = budget.AmountLimit,
            CurrencyCode = budget.CurrencyCode,
            StartDate = budget.StartDate,
            EndDate = budget.EndDate
        };
    }

    public async Task<BudgetResponse?> UpdateBudgetAsync(int userId, int budgetId, UpdateBudgetRequest request)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.Status != 0);

        if (budget == null)
            return null;

        if (budget.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this budget");

        if (request.Name != null) budget.Name = request.Name;
        if (request.Description != null) budget.Description = request.Description;
        if (request.AmountLimit.HasValue) budget.AmountLimit = request.AmountLimit.Value;
        if (request.CurrencyCode != null) budget.CurrencyCode = request.CurrencyCode;
        if (request.StartDate.HasValue) budget.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) budget.EndDate = request.EndDate.Value;

        await _context.SaveChangesAsync();

        return new BudgetResponse
        {
            Id = budget.Id,
            UserId = budget.UserId,
            Name = budget.Name,
            Description = budget.Description,
            AmountLimit = budget.AmountLimit,
            CurrencyCode = budget.CurrencyCode,
            StartDate = budget.StartDate,
            EndDate = budget.EndDate
        };
    }

    public async Task<bool> DeleteBudgetAsync(int userId, int budgetId)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.Status != 0);

        if (budget == null)
            return false;

        if (budget.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this budget");

        budget.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
