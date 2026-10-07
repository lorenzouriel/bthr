using Microsoft.EntityFrameworkCore;
using bthr.Api.Data;
using bthr.Api.DTOs;
using bthr.Api.Models;

namespace bthr.Api.Services;

public interface IHabitService
{
    Task<List<HabitResponse>> GetUserHabitsAsync(int userId);
    Task<HabitResponse> CreateHabitAsync(int userId, CreateHabitRequest request);
    Task<HabitResponse?> UpdateHabitAsync(int userId, int id, UpdateHabitRequest request);
    Task<bool> DeleteHabitAsync(int userId, int id);
}

public class HabitService : IHabitService
{
    private readonly ApplicationDbContext _context;

    public HabitService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<HabitResponse>> GetUserHabitsAsync(int userId)
    {
        var query = _context.Habits.AsNoTracking().Where(entity => entity.UserId == userId && entity.Status != 0);

        return await query.OrderByDescending(entity => entity.CreatedAt).ThenByDescending(entity => entity.Id)
            .Select(entity => new HabitResponse
            {
                Id = entity.Id,
                UserId = entity.UserId,
                HabitName = entity.HabitName,
                Category = entity.Category,
                TargetFrequency = entity.TargetFrequency,
                Description = entity.Description,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt
            }).ToListAsync();
    }

    public async Task<HabitResponse> CreateHabitAsync(int userId, CreateHabitRequest request)
    {
        if (await _context.Habits.AnyAsync(h => h.UserId == userId && h.HabitName == request.HabitName))
            throw new BodyTrackingConflictException("A habit with this name already exists, including archived habits.");
        var entity = new Habit
        {
            UserId = userId,
            HabitName = request.HabitName,
            Category = request.Category,
            TargetFrequency = request.TargetFrequency,
            Description = request.Description,
        };
        _context.Habits.Add(entity);
        await BodyTrackingPersistence.SaveChangesAsync(_context);
        return ToResponse(entity);
    }

    public async Task<HabitResponse?> UpdateHabitAsync(int userId, int id, UpdateHabitRequest request)
    {
        var entity = await _context.Habits.FirstOrDefaultAsync(entity => entity.Id == id && entity.Status != 0);
        if (entity == null) return null;
        if (entity.UserId != userId) throw new UnauthorizedAccessException();
        var name = request.HabitName ?? entity.HabitName;
        if (await _context.Habits.AnyAsync(h => h.UserId == userId && h.HabitName == name && h.Id != entity.Id))
            throw new BodyTrackingConflictException("A habit with this name already exists, including archived habits.");

        if (request.HabitName != null) entity.HabitName = request.HabitName;
        if (request.Category != null) entity.Category = request.Category;
        if (request.TargetFrequency != null) entity.TargetFrequency = request.TargetFrequency;
        if (request.Description != null) entity.Description = request.Description;
        if (request.Status.HasValue) entity.Status = request.Status.Value;
        await BodyTrackingPersistence.SaveChangesAsync(_context);
        return ToResponse(entity);
    }

    public async Task<bool> DeleteHabitAsync(int userId, int id)
    {
        var entity = await _context.Habits.FirstOrDefaultAsync(entity => entity.Id == id && entity.Status != 0);
        if (entity == null) return false;
        if (entity.UserId != userId) throw new UnauthorizedAccessException();

        entity.Status = 0;
        await _context.SaveChangesAsync();
        return true;
    }

    private static HabitResponse ToResponse(Habit entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        HabitName = entity.HabitName,
        Category = entity.Category,
        TargetFrequency = entity.TargetFrequency,
        Description = entity.Description,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt
    };
}
