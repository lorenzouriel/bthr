using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IWaterIntakeService
{
    Task<List<WaterIntakeResponse>> GetUserWaterIntakeAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<WaterIntakeResponse> CreateWaterIntakeAsync(int userId, CreateWaterIntakeRequest request);
    Task<WaterIntakeResponse?> UpdateWaterIntakeAsync(int userId, int waterIntakeId, UpdateWaterIntakeRequest request);
    Task<bool> DeleteWaterIntakeAsync(int userId, int waterIntakeId);
}

public class WaterIntakeService : IWaterIntakeService
{
    private readonly ApplicationDbContext _context;

    public WaterIntakeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WaterIntakeResponse>> GetUserWaterIntakeAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.WaterIntakes.Where(w => w.UserId == userId && w.Status != 0);

        if (startDate.HasValue)
            query = query.Where(w => w.IntakeDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(w => w.IntakeDate <= endDate.Value);

        return await query
            .OrderByDescending(w => w.IntakeDate)
            .Select(w => new WaterIntakeResponse
            {
                Id = w.Id,
                UserId = w.UserId,
                IntakeDate = w.IntakeDate,
                AmountMl = w.AmountMl,
                Status = w.Status,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<WaterIntakeResponse> CreateWaterIntakeAsync(int userId, CreateWaterIntakeRequest request)
    {
        var waterIntake = new WaterIntake
        {
            UserId = userId,
            IntakeDate = request.IntakeDate,
            AmountMl = request.AmountMl,
            Status = 1
        };

        _context.WaterIntakes.Add(waterIntake);
        await _context.SaveChangesAsync();

        return new WaterIntakeResponse
        {
            Id = waterIntake.Id,
            UserId = waterIntake.UserId,
            IntakeDate = waterIntake.IntakeDate,
            AmountMl = waterIntake.AmountMl,
            Status = waterIntake.Status,
            CreatedAt = waterIntake.CreatedAt
        };
    }

    public async Task<WaterIntakeResponse?> UpdateWaterIntakeAsync(int userId, int waterIntakeId, UpdateWaterIntakeRequest request)
    {
        var waterIntake = await _context.WaterIntakes.FirstOrDefaultAsync(w => w.Id == waterIntakeId && w.Status != 0);

        if (waterIntake == null)
            return null;

        if (waterIntake.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this water intake record");

        if (request.IntakeDate.HasValue) waterIntake.IntakeDate = request.IntakeDate.Value;
        if (request.AmountMl.HasValue) waterIntake.AmountMl = request.AmountMl.Value;
        if (request.Status.HasValue) waterIntake.Status = request.Status.Value;

        await _context.SaveChangesAsync();

        return new WaterIntakeResponse
        {
            Id = waterIntake.Id,
            UserId = waterIntake.UserId,
            IntakeDate = waterIntake.IntakeDate,
            AmountMl = waterIntake.AmountMl,
            Status = waterIntake.Status,
            CreatedAt = waterIntake.CreatedAt
        };
    }

    public async Task<bool> DeleteWaterIntakeAsync(int userId, int waterIntakeId)
    {
        var waterIntake = await _context.WaterIntakes.FirstOrDefaultAsync(w => w.Id == waterIntakeId && w.Status != 0);

        if (waterIntake == null)
            return false;

        if (waterIntake.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this water intake record");

        waterIntake.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
