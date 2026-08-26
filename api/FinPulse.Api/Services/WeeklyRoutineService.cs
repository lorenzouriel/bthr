using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IWeeklyRoutineService
{
    Task<List<WeeklyRoutineResponse>> GetUserWeeklyRoutinesAsync(int userId);
    Task<WeeklyRoutineResponse> CreateWeeklyRoutineAsync(int userId, CreateWeeklyRoutineRequest request);
    Task<WeeklyRoutineResponse?> UpdateWeeklyRoutineAsync(int userId, int routineId, UpdateWeeklyRoutineRequest request);
    Task<bool> DeleteWeeklyRoutineAsync(int userId, int routineId);
}

public class WeeklyRoutineService : IWeeklyRoutineService
{
    private readonly ApplicationDbContext _context;

    public WeeklyRoutineService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WeeklyRoutineResponse>> GetUserWeeklyRoutinesAsync(int userId)
    {
        return await _context.WeeklyRoutines
            .Where(r => r.UserId == userId && r.Status != 0)
            .OrderBy(r => r.DayOfWeek)
            .Select(r => new WeeklyRoutineResponse
            {
                Id = r.Id,
                UserId = r.UserId,
                DayOfWeek = r.DayOfWeek,
                RoutineName = r.RoutineName,
                Description = r.Description,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<WeeklyRoutineResponse> CreateWeeklyRoutineAsync(int userId, CreateWeeklyRoutineRequest request)
    {
        var routine = new WeeklyRoutine
        {
            UserId = userId,
            DayOfWeek = request.DayOfWeek,
            RoutineName = request.RoutineName,
            Description = request.Description,
            Status = 1
        };

        _context.WeeklyRoutines.Add(routine);
        await _context.SaveChangesAsync();

        return new WeeklyRoutineResponse
        {
            Id = routine.Id,
            UserId = routine.UserId,
            DayOfWeek = routine.DayOfWeek,
            RoutineName = routine.RoutineName,
            Description = routine.Description,
            Status = routine.Status,
            CreatedAt = routine.CreatedAt
        };
    }

    public async Task<WeeklyRoutineResponse?> UpdateWeeklyRoutineAsync(int userId, int routineId, UpdateWeeklyRoutineRequest request)
    {
        var routine = await _context.WeeklyRoutines.FirstOrDefaultAsync(r => r.Id == routineId && r.Status != 0);

        if (routine == null)
            return null;

        if (routine.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this weekly routine");

        if (request.DayOfWeek.HasValue) routine.DayOfWeek = request.DayOfWeek.Value;
        if (request.RoutineName != null) routine.RoutineName = request.RoutineName;
        if (request.Description != null) routine.Description = request.Description;
        if (request.Status.HasValue) routine.Status = request.Status.Value;

        await _context.SaveChangesAsync();

        return new WeeklyRoutineResponse
        {
            Id = routine.Id,
            UserId = routine.UserId,
            DayOfWeek = routine.DayOfWeek,
            RoutineName = routine.RoutineName,
            Description = routine.Description,
            Status = routine.Status,
            CreatedAt = routine.CreatedAt
        };
    }

    public async Task<bool> DeleteWeeklyRoutineAsync(int userId, int routineId)
    {
        var routine = await _context.WeeklyRoutines.FirstOrDefaultAsync(r => r.Id == routineId && r.Status != 0);

        if (routine == null)
            return false;

        if (routine.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this weekly routine");

        routine.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
