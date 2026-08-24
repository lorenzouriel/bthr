using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IEarningService
{
    Task<List<EarningResponse>> GetUserEarningsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, string? category = null);
    Task<EarningResponse> CreateEarningAsync(int userId, CreateEarningRequest request);
    Task<EarningResponse?> UpdateEarningAsync(int userId, int earningId, UpdateEarningRequest request);
    Task<bool> DeleteEarningAsync(int userId, int earningId);
}

public class EarningService : IEarningService
{
    private readonly ApplicationDbContext _context;

    public EarningService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<EarningResponse>> GetUserEarningsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, string? category = null)
    {
        var query = _context.Earnings
            .Where(e => e.UserId == userId && e.Status != 0);

        if (startDate.HasValue)
            query = query.Where(e => e.EarningDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.EarningDate <= endDate.Value);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(e => e.Category == category);

        return await query
            .OrderByDescending(e => e.EarningDate)
            .Select(e => new EarningResponse
            {
                Id = e.Id,
                UserId = e.UserId,
                Category = e.Category,
                PaymentMethod = e.PaymentMethod,
                CurrencyCode = e.CurrencyCode,
                Amount = e.Amount,
                Description = e.Description,
                EarningDate = e.EarningDate
            })
            .ToListAsync();
    }

    public async Task<EarningResponse> CreateEarningAsync(int userId, CreateEarningRequest request)
    {
        var earning = new Earning
        {
            UserId = userId,
            Category = request.Category,
            PaymentMethod = request.PaymentMethod,
            CurrencyCode = request.CurrencyCode,
            Amount = request.Amount,
            Description = request.Description,
            EarningDate = request.EarningDate,
            Status = 1
        };

        _context.Earnings.Add(earning);
        await _context.SaveChangesAsync();

        return new EarningResponse
        {
            Id = earning.Id,
            UserId = earning.UserId,
            Category = earning.Category,
            PaymentMethod = earning.PaymentMethod,
            CurrencyCode = earning.CurrencyCode,
            Amount = earning.Amount,
            Description = earning.Description,
            EarningDate = earning.EarningDate
        };
    }

    public async Task<EarningResponse?> UpdateEarningAsync(int userId, int earningId, UpdateEarningRequest request)
    {
        var earning = await _context.Earnings
            .FirstOrDefaultAsync(e => e.Id == earningId && e.Status != 0);

        if (earning == null)
            return null;

        if (earning.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this earning");

        if (request.Category != null) earning.Category = request.Category;
        if (request.PaymentMethod != null) earning.PaymentMethod = request.PaymentMethod;
        if (request.CurrencyCode != null) earning.CurrencyCode = request.CurrencyCode;
        if (request.Amount.HasValue) earning.Amount = request.Amount.Value;
        if (request.Description != null) earning.Description = request.Description;
        if (request.EarningDate.HasValue) earning.EarningDate = request.EarningDate.Value;

        await _context.SaveChangesAsync();

        return new EarningResponse
        {
            Id = earning.Id,
            UserId = earning.UserId,
            Category = earning.Category,
            PaymentMethod = earning.PaymentMethod,
            CurrencyCode = earning.CurrencyCode,
            Amount = earning.Amount,
            Description = earning.Description,
            EarningDate = earning.EarningDate
        };
    }

    public async Task<bool> DeleteEarningAsync(int userId, int earningId)
    {
        var earning = await _context.Earnings
            .FirstOrDefaultAsync(e => e.Id == earningId && e.Status != 0);

        if (earning == null)
            return false;

        if (earning.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this earning");

        earning.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
