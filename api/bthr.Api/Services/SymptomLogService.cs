using Microsoft.EntityFrameworkCore;
using bthr.Api.Data;
using bthr.Api.DTOs;
using bthr.Api.Models;

namespace bthr.Api.Services;

public interface ISymptomLogService
{
    Task<List<SymptomLogResponse>> GetUserSymptomLogsAsync(int userId, DateOnly? startDate = null, DateOnly? endDate = null);
    Task<SymptomLogResponse> CreateSymptomLogAsync(int userId, CreateSymptomLogRequest request);
}

public class SymptomLogService : ISymptomLogService
{
    private readonly ApplicationDbContext _context;

    public SymptomLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SymptomLogResponse>> GetUserSymptomLogsAsync(int userId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var query = _context.SymptomLogs.AsNoTracking().Where(entity => entity.UserId == userId && entity.Status != 0);

        if (startDate.HasValue) query = query.Where(entity => entity.LogDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(entity => entity.LogDate <= endDate.Value);

        return await query.OrderByDescending(entity => entity.LogDate).ThenByDescending(entity => entity.Id)
            .Select(entity => new SymptomLogResponse
            {
                Id = entity.Id,
                UserId = entity.UserId,
                LogDate = entity.LogDate,
                Symptom = entity.Symptom,
                Severity = entity.Severity,
                Notes = entity.Notes,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt
            }).ToListAsync();
    }

    public async Task<SymptomLogResponse> CreateSymptomLogAsync(int userId, CreateSymptomLogRequest request)
    {
        var entity = new SymptomLog
        {
            UserId = userId,
            LogDate = request.LogDate!.Value,
            Symptom = request.Symptom,
            Severity = request.Severity,
            Notes = request.Notes,
        };
        _context.SymptomLogs.Add(entity);
        await _context.SaveChangesAsync();
        return ToResponse(entity);
    }

    private static SymptomLogResponse ToResponse(SymptomLog entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        LogDate = entity.LogDate,
        Symptom = entity.Symptom,
        Severity = entity.Severity,
        Notes = entity.Notes,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt
    };
}
