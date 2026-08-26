using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface ISleepLogService
{
    Task<List<SleepLogResponse>> GetUserSleepLogsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<SleepLogResponse> CreateSleepLogAsync(int userId, CreateSleepLogRequest request);
    Task<SleepLogResponse?> UpdateSleepLogAsync(int userId, int sleepLogId, UpdateSleepLogRequest request);
    Task<bool> DeleteSleepLogAsync(int userId, int sleepLogId);
}

public class SleepLogService : ISleepLogService
{
    private readonly ApplicationDbContext _context;

    public SleepLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SleepLogResponse>> GetUserSleepLogsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.SleepLogs.Where(s => s.UserId == userId && s.Status != 0);

        if (startDate.HasValue)
            query = query.Where(s => s.BedTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.BedTime <= endDate.Value);

        return await query
            .OrderByDescending(s => s.BedTime)
            .Select(s => new SleepLogResponse
            {
                Id = s.Id,
                UserId = s.UserId,
                BedTime = s.BedTime,
                WakeTime = s.WakeTime,
                TotalHours = s.TotalHours,
                Notes = s.Notes,
                Status = s.Status,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<SleepLogResponse> CreateSleepLogAsync(int userId, CreateSleepLogRequest request)
    {
        var sleepLog = new SleepLog
        {
            UserId = userId,
            BedTime = request.BedTime,
            WakeTime = request.WakeTime,
            Notes = request.Notes,
            Status = 1
        };

        _context.SleepLogs.Add(sleepLog);
        await _context.SaveChangesAsync();

        return new SleepLogResponse
        {
            Id = sleepLog.Id,
            UserId = sleepLog.UserId,
            BedTime = sleepLog.BedTime,
            WakeTime = sleepLog.WakeTime,
            TotalHours = sleepLog.TotalHours,
            Notes = sleepLog.Notes,
            Status = sleepLog.Status,
            CreatedAt = sleepLog.CreatedAt
        };
    }

    public async Task<SleepLogResponse?> UpdateSleepLogAsync(int userId, int sleepLogId, UpdateSleepLogRequest request)
    {
        var sleepLog = await _context.SleepLogs.FirstOrDefaultAsync(s => s.Id == sleepLogId && s.Status != 0);

        if (sleepLog == null)
            return null;

        if (sleepLog.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this sleep log");

        if (request.BedTime.HasValue) sleepLog.BedTime = request.BedTime.Value;
        if (request.WakeTime.HasValue) sleepLog.WakeTime = request.WakeTime.Value;
        if (request.Notes != null) sleepLog.Notes = request.Notes;
        if (request.Status.HasValue) sleepLog.Status = request.Status.Value;

        await _context.SaveChangesAsync();

        return new SleepLogResponse
        {
            Id = sleepLog.Id,
            UserId = sleepLog.UserId,
            BedTime = sleepLog.BedTime,
            WakeTime = sleepLog.WakeTime,
            TotalHours = sleepLog.TotalHours,
            Notes = sleepLog.Notes,
            Status = sleepLog.Status,
            CreatedAt = sleepLog.CreatedAt
        };
    }

    public async Task<bool> DeleteSleepLogAsync(int userId, int sleepLogId)
    {
        var sleepLog = await _context.SleepLogs.FirstOrDefaultAsync(s => s.Id == sleepLogId && s.Status != 0);

        if (sleepLog == null)
            return false;

        if (sleepLog.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this sleep log");

        sleepLog.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
