using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IInvestmentService
{
    Task<List<InvestmentResponse>> GetUserInvestmentsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, string? investmentType = null, string? category = null);
    Task<InvestmentResponse> CreateInvestmentAsync(int userId, CreateInvestmentRequest request);
    Task<InvestmentResponse?> UpdateInvestmentAsync(int userId, int investmentId, UpdateInvestmentRequest request);
    Task<bool> DeleteInvestmentAsync(int userId, int investmentId);
}

public class InvestmentService : IInvestmentService
{
    private readonly ApplicationDbContext _context;

    public InvestmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<InvestmentResponse>> GetUserInvestmentsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, string? investmentType = null, string? category = null)
    {
        var query = _context.Investments
            .Where(i => i.UserId == userId && i.Status != 0);

        if (startDate.HasValue)
            query = query.Where(i => i.PurchaseDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(i => i.PurchaseDate <= endDate.Value);

        if (!string.IsNullOrEmpty(investmentType))
            query = query.Where(i => i.InvestmentType == investmentType);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(i => i.Category == category);

        return await query
            .OrderByDescending(i => i.PurchaseDate)
            .Select(i => new InvestmentResponse
            {
                Id = i.Id,
                UserId = i.UserId,
                InvestmentType = i.InvestmentType,
                Category = i.Category,
                AssetName = i.AssetName,
                Broker = i.Broker,
                CurrencyCode = i.CurrencyCode,
                InvestedAmount = i.InvestedAmount,
                CurrentValue = i.CurrentValue,
                PurchaseDate = i.PurchaseDate,
                MaturityDate = i.MaturityDate,
                AnnualYieldPercent = i.AnnualYieldPercent,
                ProfitLoss = i.ProfitLoss
            })
            .ToListAsync();
    }

    public async Task<InvestmentResponse> CreateInvestmentAsync(int userId, CreateInvestmentRequest request)
    {
        var investment = new Investment
        {
            UserId = userId,
            InvestmentType = request.InvestmentType,
            Category = request.Category,
            AssetName = request.AssetName,
            Broker = request.Broker,
            CurrencyCode = request.CurrencyCode,
            InvestedAmount = request.InvestedAmount,
            CurrentValue = request.CurrentValue,
            PurchaseDate = request.PurchaseDate,
            MaturityDate = request.MaturityDate,
            AnnualYieldPercent = request.AnnualYieldPercent,
            ProfitLoss = request.ProfitLoss,
            Status = 1
        };

        _context.Investments.Add(investment);
        await _context.SaveChangesAsync();

        return new InvestmentResponse
        {
            Id = investment.Id,
            UserId = investment.UserId,
            InvestmentType = investment.InvestmentType,
            Category = investment.Category,
            AssetName = investment.AssetName,
            Broker = investment.Broker,
            CurrencyCode = investment.CurrencyCode,
            InvestedAmount = investment.InvestedAmount,
            CurrentValue = investment.CurrentValue,
            PurchaseDate = investment.PurchaseDate,
            MaturityDate = investment.MaturityDate,
            AnnualYieldPercent = investment.AnnualYieldPercent,
            ProfitLoss = investment.ProfitLoss
        };
    }

    public async Task<InvestmentResponse?> UpdateInvestmentAsync(int userId, int investmentId, UpdateInvestmentRequest request)
    {
        var investment = await _context.Investments
            .FirstOrDefaultAsync(i => i.Id == investmentId && i.Status != 0);

        if (investment == null)
            return null;

        if (investment.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this investment");

        if (request.InvestmentType != null) investment.InvestmentType = request.InvestmentType;
        if (request.Category != null) investment.Category = request.Category;
        if (request.AssetName != null) investment.AssetName = request.AssetName;
        if (request.Broker != null) investment.Broker = request.Broker;
        if (request.CurrencyCode != null) investment.CurrencyCode = request.CurrencyCode;
        if (request.InvestedAmount.HasValue) investment.InvestedAmount = request.InvestedAmount.Value;
        if (request.CurrentValue.HasValue) investment.CurrentValue = request.CurrentValue;
        if (request.PurchaseDate.HasValue) investment.PurchaseDate = request.PurchaseDate.Value;
        if (request.MaturityDate.HasValue) investment.MaturityDate = request.MaturityDate;
        if (request.AnnualYieldPercent.HasValue) investment.AnnualYieldPercent = request.AnnualYieldPercent;
        if (request.ProfitLoss.HasValue) investment.ProfitLoss = request.ProfitLoss;

        await _context.SaveChangesAsync();

        return new InvestmentResponse
        {
            Id = investment.Id,
            UserId = investment.UserId,
            InvestmentType = investment.InvestmentType,
            Category = investment.Category,
            AssetName = investment.AssetName,
            Broker = investment.Broker,
            CurrencyCode = investment.CurrencyCode,
            InvestedAmount = investment.InvestedAmount,
            CurrentValue = investment.CurrentValue,
            PurchaseDate = investment.PurchaseDate,
            MaturityDate = investment.MaturityDate,
            AnnualYieldPercent = investment.AnnualYieldPercent,
            ProfitLoss = investment.ProfitLoss
        };
    }

    public async Task<bool> DeleteInvestmentAsync(int userId, int investmentId)
    {
        var investment = await _context.Investments
            .FirstOrDefaultAsync(i => i.Id == investmentId && i.Status != 0);

        if (investment == null)
            return false;

        if (investment.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this investment");

        investment.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
