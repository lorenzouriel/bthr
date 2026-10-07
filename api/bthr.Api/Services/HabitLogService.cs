using Microsoft.EntityFrameworkCore;
using bthr.Api.Data;
using bthr.Api.DTOs;
using bthr.Api.Models;

namespace bthr.Api.Services;

public interface IHabitLogService
{
    Task<List<HabitLogResponse>> GetUserHabitLogsAsync(int userId, DateOnly? startDate = null, DateOnly? endDate = null, int? habitId = null);
    Task<HabitLogResponse> CreateHabitLogAsync(int userId, CreateHabitLogRequest request);
    Task<HabitLogResponse?> UpdateHabitLogAsync(int userId, int id, UpdateHabitLogRequest request);
    Task<bool> DeleteHabitLogAsync(int userId, int id);
}

public class HabitLogService : IHabitLogService
{
    private readonly ApplicationDbContext _context;

    public HabitLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<HabitLogResponse>> GetUserHabitLogsAsync(int userId, DateOnly? startDate = null, DateOnly? endDate = null, int? habitId = null)
    {
        var query = _context.HabitLogs.AsNoTracking().Where(entity => entity.UserId == userId && entity.Status != 0);

        if (startDate.HasValue) query = query.Where(entity => entity.LogDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(entity => entity.LogDate <= endDate.Value);
        if (habitId.HasValue) query = query.Where(entity => entity.HabitId == habitId.Value);

        return await query.OrderByDescending(entity => entity.LogDate).ThenByDescending(entity => entity.Id)
            .Select(entity => new HabitLogResponse
            {
                Id = entity.Id,
                UserId = entity.UserId,
                HabitId = entity.HabitId,
                LogDate = entity.LogDate,
                IsCompleted = entity.IsCompleted,
                Notes = entity.Notes,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt
            }).ToListAsync();
    }

    public async Task<HabitLogResponse> CreateHabitLogAsync(int userId, CreateHabitLogRequest request)
    {
        await ValidateHabitAsync(userId, request.HabitId!.Value);
        await EnsureUniqueLogAsync(userId, request.HabitId.Value, request.LogDate!.Value);
        var entity = new HabitLog
        {
            UserId = userId,
            HabitId = request.HabitId.Value,
            LogDate = request.LogDate.Value,
            IsCompleted = request.IsCompleted,
            Notes = request.Notes,
        };
        _context.HabitLogs.Add(entity);
        await BodyTrackingPersistence.SaveChangesAsync(_context);
        return ToResponse(entity);
    }

    public async Task<HabitLogResponse?> UpdateHabitLogAsync(int userId, int id, UpdateHabitLogRequest request)
    {
        var entity = await _context.HabitLogs.FirstOrDefaultAsync(entity => entity.Id == id && entity.Status != 0);
        if (entity == null) return null;
        if (entity.UserId != userId) throw new UnauthorizedAccessException();
        // Existing logs remain editable after their habit is archived.
        if (request.HabitId.HasValue && request.HabitId.Value != entity.HabitId)
            await ValidateHabitAsync(userId, request.HabitId.Value);
        await EnsureUniqueLogAsync(userId, request.HabitId ?? entity.HabitId, request.LogDate ?? entity.LogDate, entity.Id);

        if (request.HabitId.HasValue) entity.HabitId = request.HabitId.Value;
        if (request.LogDate.HasValue) entity.LogDate = request.LogDate.Value;
        if (request.IsCompleted.HasValue) entity.IsCompleted = request.IsCompleted.Value;
        if (request.Notes != null) entity.Notes = request.Notes;
        if (request.Status.HasValue) entity.Status = request.Status.Value;
        await BodyTrackingPersistence.SaveChangesAsync(_context);
        return ToResponse(entity);
    }

    public async Task<bool> DeleteHabitLogAsync(int userId, int id)
    {
        var entity = await _context.HabitLogs.FirstOrDefaultAsync(entity => entity.Id == id && entity.Status != 0);
        if (entity == null) return false;
        if (entity.UserId != userId) throw new UnauthorizedAccessException();

        entity.Status = 0;
        await _context.SaveChangesAsync();
        return true;
    }

    private static HabitLogResponse ToResponse(HabitLog entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        HabitId = entity.HabitId,
        LogDate = entity.LogDate,
        IsCompleted = entity.IsCompleted,
        Notes = entity.Notes,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt
    };

    private async Task ValidateHabitAsync(int userId, int habitId)
    {
        if (!await _context.Habits.AnyAsync(h => h.Id == habitId && h.UserId == userId && h.Status != 0))
            throw new InvalidHabitException("Habit must be active and belong to the current user.");
    }

    private async Task EnsureUniqueLogAsync(int userId, int habitId, DateOnly date, int? exceptId = null)
    {
        // The database uniqueness constraint also includes soft-deleted logs.
        if (await _context.HabitLogs.AnyAsync(log => log.UserId == userId && log.HabitId == habitId
            && log.LogDate == date && (!exceptId.HasValue || log.Id != exceptId.Value)))
            throw new BodyTrackingConflictException("A log already exists for this habit and date, including deleted logs.");
    }
}
