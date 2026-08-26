using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IWorkoutService
{
    Task<List<WorkoutResponse>> GetUserWorkoutsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<WorkoutResponse> CreateWorkoutAsync(int userId, CreateWorkoutRequest request);
    Task<WorkoutResponse?> UpdateWorkoutAsync(int userId, int workoutId, UpdateWorkoutRequest request);
    Task<bool> DeleteWorkoutAsync(int userId, int workoutId);
}

public class WorkoutService : IWorkoutService
{
    private readonly ApplicationDbContext _context;

    public WorkoutService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkoutResponse>> GetUserWorkoutsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Workouts.Where(w => w.UserId == userId && w.Status != 0);

        if (startDate.HasValue)
            query = query.Where(w => w.WorkoutDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(w => w.WorkoutDate <= endDate.Value);

        return await query
            .OrderByDescending(w => w.WorkoutDate)
            .Select(w => new WorkoutResponse
            {
                Id = w.Id,
                UserId = w.UserId,
                WorkoutDate = w.WorkoutDate,
                RoutineName = w.RoutineName,
                DurationMinutes = w.DurationMinutes,
                CaloriesBurned = w.CaloriesBurned,
                Notes = w.Notes,
                Status = w.Status,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<WorkoutResponse> CreateWorkoutAsync(int userId, CreateWorkoutRequest request)
    {
        var workout = new Workout
        {
            UserId = userId,
            WorkoutDate = request.WorkoutDate,
            RoutineName = request.RoutineName,
            DurationMinutes = request.DurationMinutes,
            CaloriesBurned = request.CaloriesBurned,
            Notes = request.Notes,
            Status = 1
        };

        _context.Workouts.Add(workout);
        await _context.SaveChangesAsync();

        return new WorkoutResponse
        {
            Id = workout.Id,
            UserId = workout.UserId,
            WorkoutDate = workout.WorkoutDate,
            RoutineName = workout.RoutineName,
            DurationMinutes = workout.DurationMinutes,
            CaloriesBurned = workout.CaloriesBurned,
            Notes = workout.Notes,
            Status = workout.Status,
            CreatedAt = workout.CreatedAt
        };
    }

    public async Task<WorkoutResponse?> UpdateWorkoutAsync(int userId, int workoutId, UpdateWorkoutRequest request)
    {
        var workout = await _context.Workouts.FirstOrDefaultAsync(w => w.Id == workoutId && w.Status != 0);

        if (workout == null)
            return null;

        if (workout.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this workout");

        if (request.WorkoutDate.HasValue) workout.WorkoutDate = request.WorkoutDate.Value;
        if (request.RoutineName != null) workout.RoutineName = request.RoutineName;
        if (request.DurationMinutes.HasValue) workout.DurationMinutes = request.DurationMinutes.Value;
        if (request.CaloriesBurned.HasValue) workout.CaloriesBurned = request.CaloriesBurned.Value;
        if (request.Notes != null) workout.Notes = request.Notes;
        if (request.Status.HasValue) workout.Status = request.Status.Value;

        await _context.SaveChangesAsync();

        return new WorkoutResponse
        {
            Id = workout.Id,
            UserId = workout.UserId,
            WorkoutDate = workout.WorkoutDate,
            RoutineName = workout.RoutineName,
            DurationMinutes = workout.DurationMinutes,
            CaloriesBurned = workout.CaloriesBurned,
            Notes = workout.Notes,
            Status = workout.Status,
            CreatedAt = workout.CreatedAt
        };
    }

    public async Task<bool> DeleteWorkoutAsync(int userId, int workoutId)
    {
        var workout = await _context.Workouts.FirstOrDefaultAsync(w => w.Id == workoutId && w.Status != 0);

        if (workout == null)
            return false;

        if (workout.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this workout");

        workout.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
