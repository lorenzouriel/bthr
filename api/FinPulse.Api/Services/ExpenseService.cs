using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IExpenseService
{
    Task<List<ExpenseResponse>> GetUserExpensesAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, string? category = null);
    Task<ExpenseResponse> CreateExpenseAsync(int userId, CreateExpenseRequest request);
    Task<ExpenseResponse?> UpdateExpenseAsync(int userId, int expenseId, UpdateExpenseRequest request);
    Task<bool> DeleteExpenseAsync(int userId, int expenseId);
}

public class ExpenseService : IExpenseService
{
    private readonly ApplicationDbContext _context;

    public ExpenseService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ExpenseResponse>> GetUserExpensesAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, string? category = null)
    {
        var query = _context.Expenses
            .Where(e => e.UserId == userId && e.Status != 0);

        if (startDate.HasValue)
            query = query.Where(e => e.ExpenseDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.ExpenseDate <= endDate.Value);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(e => e.Category == category);

        return await query
            .OrderByDescending(e => e.ExpenseDate)
            .Select(e => new ExpenseResponse
            {
                Id = e.Id,
                UserId = e.UserId,
                Category = e.Category,
                PaymentMethod = e.PaymentMethod,
                CurrencyCode = e.CurrencyCode,
                Amount = e.Amount,
                Description = e.Description,
                ExpenseDate = e.ExpenseDate
            })
            .ToListAsync();
    }

    public async Task<ExpenseResponse> CreateExpenseAsync(int userId, CreateExpenseRequest request)
    {
        var expense = new Expense
        {
            UserId = userId,
            Category = request.Category,
            PaymentMethod = request.PaymentMethod,
            CurrencyCode = request.CurrencyCode,
            Amount = request.Amount,
            Description = request.Description,
            ExpenseDate = request.ExpenseDate,
            Status = 1
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        return new ExpenseResponse
        {
            Id = expense.Id,
            UserId = expense.UserId,
            Category = expense.Category,
            PaymentMethod = expense.PaymentMethod,
            CurrencyCode = expense.CurrencyCode,
            Amount = expense.Amount,
            Description = expense.Description,
            ExpenseDate = expense.ExpenseDate
        };
    }

    public async Task<ExpenseResponse?> UpdateExpenseAsync(int userId, int expenseId, UpdateExpenseRequest request)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == expenseId && e.Status != 0);

        if (expense == null)
            return null;

        if (expense.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this expense");

        if (request.Category != null) expense.Category = request.Category;
        if (request.PaymentMethod != null) expense.PaymentMethod = request.PaymentMethod;
        if (request.CurrencyCode != null) expense.CurrencyCode = request.CurrencyCode;
        if (request.Amount.HasValue) expense.Amount = request.Amount.Value;
        if (request.Description != null) expense.Description = request.Description;
        if (request.ExpenseDate.HasValue) expense.ExpenseDate = request.ExpenseDate.Value;

        await _context.SaveChangesAsync();

        return new ExpenseResponse
        {
            Id = expense.Id,
            UserId = expense.UserId,
            Category = expense.Category,
            PaymentMethod = expense.PaymentMethod,
            CurrencyCode = expense.CurrencyCode,
            Amount = expense.Amount,
            Description = expense.Description,
            ExpenseDate = expense.ExpenseDate
        };
    }

    public async Task<bool> DeleteExpenseAsync(int userId, int expenseId)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == expenseId && e.Status != 0);

        if (expense == null)
            return false;

        if (expense.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this expense");

        expense.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
