using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface ISubstanceLogService
{
    Task<List<SubstanceLogResponse>> GetUserSubstanceLogsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<SubstanceLogResponse> CreateSubstanceLogAsync(int userId, CreateSubstanceLogRequest request);
}

public class SubstanceLogService : ISubstanceLogService
{
    private readonly ApplicationDbContext _context;

    public SubstanceLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SubstanceLogResponse>> GetUserSubstanceLogsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        if (startDate.HasValue) startDate = AsUtc(startDate.Value);
        if (endDate.HasValue) endDate = AsUtc(endDate.Value);
        var query = _context.SubstanceLogs.AsNoTracking().Where(entity => entity.UserId == userId && entity.Status != 0);

        if (startDate.HasValue) query = query.Where(entity => entity.ConsumedAt >= startDate.Value);
        if (endDate.HasValue) query = query.Where(entity => entity.ConsumedAt <= endDate.Value);

        return await query.OrderByDescending(entity => entity.ConsumedAt).ThenByDescending(entity => entity.Id)
            .Select(entity => new SubstanceLogResponse
            {
                Id = entity.Id,
                UserId = entity.UserId,
                ConsumedAt = entity.ConsumedAt,
                SubstanceType = entity.SubstanceType,
                Amount = entity.Amount,
                Unit = entity.Unit,
                Notes = entity.Notes,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt
            }).ToListAsync();
    }

    public async Task<SubstanceLogResponse> CreateSubstanceLogAsync(int userId, CreateSubstanceLogRequest request)
    {
        var entity = new SubstanceLog
        {
            UserId = userId,
            ConsumedAt = AsUtc(request.ConsumedAt!.Value),
            SubstanceType = request.SubstanceType,
            Amount = request.Amount!.Value,
            Unit = request.Unit,
            Notes = request.Notes,
        };
        _context.SubstanceLogs.Add(entity);
        await _context.SaveChangesAsync();
        return ToResponse(entity);
    }

    // Unspecified timestamps follow the API's existing UTC convention.
    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Local
        ? value.ToUniversalTime()
        : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static SubstanceLogResponse ToResponse(SubstanceLog entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        ConsumedAt = entity.ConsumedAt,
        SubstanceType = entity.SubstanceType,
        Amount = entity.Amount,
        Unit = entity.Unit,
        Notes = entity.Notes,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt
    };
}
