using Microsoft.EntityFrameworkCore;
using bthr.Api.Data;
using bthr.Api.DTOs;
using bthr.Api.Models;

namespace bthr.Api.Services;

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
        var query = _context.Bills.AsNoTracking().Where(b => b.UserId == userId && b.Status != 0);
        // Filter stored dates only. Recurrence metadata does not synthesize bill occurrences.
        if (year.HasValue) query = query.Where(b => b.DueDate.HasValue && b.DueDate.Value.Year == year.Value);
        if (month.HasValue) query = query.Where(b => b.DueDate.HasValue && b.DueDate.Value.Month == month.Value);
        var bills = await query.OrderBy(b => b.DueDate).ThenBy(b => b.Id).ToListAsync();
        return bills.Select(MapToResponse).ToList();
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
            DueDate = request.DueDate,
            RecurrenceInterval = request.RecurrenceInterval,
            NextDueDate = request.NextDueDate,
            PaidDate = request.PaidDate,
            RecurrenceType = request.RecurrenceType,
            Description = request.Description,
            Status = 1
        };

        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        return MapToResponse(bill);
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
        if (request.HasPaymentMethod) bill.PaymentMethod = request.PaymentMethod;
        if (request.Amount.HasValue) bill.Amount = request.Amount.Value;
        if (request.CurrencyCode != null) bill.CurrencyCode = request.CurrencyCode;
        if (request.HasDueDate) bill.DueDate = request.DueDate;
        if (request.HasRecurrenceInterval) bill.RecurrenceInterval = request.RecurrenceInterval;
        if (request.HasNextDueDate) bill.NextDueDate = request.NextDueDate;
        if (request.HasPaidDate) bill.PaidDate = request.PaidDate;
        if (request.HasRecurrenceType) bill.RecurrenceType = request.RecurrenceType;
        if (request.HasDescription) bill.Description = request.Description;

        bill.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToResponse(bill);
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
        bill.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    private static BillResponse MapToResponse(Bill b) => new BillResponse
    {
        Id = b.Id,
        UserId = b.UserId,
        Name = b.Name,
        Description = b.Description,
        Category = b.Category,
        Amount = b.Amount,
        DueDate = b.DueDate,
        PaymentMethod = b.PaymentMethod,
        CurrencyCode = b.CurrencyCode,
        RecurrenceInterval = b.RecurrenceInterval,
        NextDueDate = b.NextDueDate,
        PaidDate = b.PaidDate,
        UpdatedAt = b.UpdatedAt,
        RecurrenceType = b.RecurrenceType,
        Status = b.Status,
        CreatedAt = b.CreatedAt,
    };
}
