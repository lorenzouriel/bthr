using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IBillService
{
    Task<List<BillResponse>> GetUserBillsAsync(int userId, int? year = null, int? month = null);
    Task<BillResponse> CreateBillAsync(int userId, CreateBillRequest request);
    Task<BillResponse?> UpdateBillAsync(int userId, int billId, UpdateBillRequest request);
    Task<bool> DeleteBillAsync(int userId, int billId);
}

public class BillService : IBillService
{
    private readonly ApplicationDbContext _context;

    public BillService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BillResponse>> GetUserBillsAsync(int userId, int? year = null, int? month = null)
    {
        var now = DateTime.UtcNow;
        var targetYear = year ?? now.Year;
        var targetMonth = month ?? now.Month;
        var monthStart = new DateTime(targetYear, targetMonth, 1);
        var monthEnd = monthStart.AddMonths(1);

        var bills = await _context.Bills
            .Where(b => b.UserId == userId
                     && b.Status != 0
                     && b.CreatedAt < monthEnd
                     && (b.EndDate == null || b.EndDate >= monthStart))
            .OrderBy(b => b.DueDay)
            .ToListAsync();

        var billNamesList = bills.Select(b => b.Name).ToList();

        var paidExpenses = new List<Expense>();
        if (billNamesList.Count > 0)
        {
            paidExpenses = await _context.Expenses
                .Where(e => e.UserId == userId
                         && e.ExpenseDate >= monthStart
                         && e.ExpenseDate < monthEnd
                         && e.Description != null
                         && billNamesList.Contains(e.Description))
                .ToListAsync();
        }

        var paidLookup = paidExpenses
            .GroupBy(e => e.Description!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.ExpenseDate).First(), StringComparer.OrdinalIgnoreCase);

        return bills.Select(b =>
        {
            var daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);
            var day = Math.Min(b.DueDay, daysInMonth);
            var dueDate = new DateTime(targetYear, targetMonth, day);
            paidLookup.TryGetValue(b.Name, out var matchedExpense);
            return MapToResponse(b, dueDate, matchedExpense);
        }).ToList();
    }

    public async Task<BillResponse> CreateBillAsync(int userId, CreateBillRequest request)
    {
        var bill = new Bill
        {
            UserId = userId,
            Name = request.Name,
            Category = request.Category,
            PaymentMethod = request.PaymentMethod,
            Amount = request.Amount,
            CurrencyCode = request.CurrencyCode,
            DueDay = request.DueDay,
            IsRecurrent = request.IsRecurrent,
            EndDate = request.EndDate,
            RecurrenceType = request.RecurrenceType,
            Description = request.Description,
            Status = 1
        };

        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
        var day = Math.Min(bill.DueDay, daysInMonth);
        return MapToResponse(bill, new DateTime(now.Year, now.Month, day), null);
    }

    public async Task<BillResponse?> UpdateBillAsync(int userId, int billId, UpdateBillRequest request)
    {
        var bill = await _context.Bills
            .FirstOrDefaultAsync(b => b.Id == billId && b.Status != 0);

        if (bill == null)
            return null;

        if (bill.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this bill");

        if (request.Name != null) bill.Name = request.Name;
        if (request.Category != null) bill.Category = request.Category;
        if (request.PaymentMethod != null) bill.PaymentMethod = request.PaymentMethod;
        if (request.Amount.HasValue) bill.Amount = request.Amount.Value;
        if (request.CurrencyCode != null) bill.CurrencyCode = request.CurrencyCode;
        if (request.DueDay.HasValue) bill.DueDay = request.DueDay.Value;
        if (request.IsRecurrent.HasValue) bill.IsRecurrent = request.IsRecurrent.Value;
        if (request.EndDate.HasValue) bill.EndDate = request.EndDate;
        if (request.RecurrenceType != null) bill.RecurrenceType = request.RecurrenceType;
        if (request.Description != null) bill.Description = request.Description;

        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
        var day = Math.Min(bill.DueDay, daysInMonth);
        return MapToResponse(bill, new DateTime(now.Year, now.Month, day), null);
    }

    public async Task<bool> DeleteBillAsync(int userId, int billId)
    {
        var bill = await _context.Bills
            .FirstOrDefaultAsync(b => b.Id == billId && b.Status != 0);

        if (bill == null)
            return false;

        if (bill.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this bill");

        bill.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }

    private static BillResponse MapToResponse(Bill b, DateTime dueDate, Expense? paidExpense) => new BillResponse
    {
        Id = b.Id,
        UserId = b.UserId,
        Name = b.Name,
        Description = b.Description,
        Category = b.Category,
        Amount = b.Amount,
        DueDay = b.DueDay,
        PaymentMethod = b.PaymentMethod,
        CurrencyCode = b.CurrencyCode,
        IsRecurrent = b.IsRecurrent,
        EndDate = b.EndDate,
        RecurrenceType = b.RecurrenceType,
        Status = b.Status,
        CreatedAt = b.CreatedAt,
        DueDate = dueDate.ToString("yyyy-MM-dd"),
        PaidThisMonth = paidExpense != null,
        PaidDate = paidExpense?.ExpenseDate.ToString("yyyy-MM-dd"),
    };
}
