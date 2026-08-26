using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IBodyMetricService
{
    Task<List<BodyMetricResponse>> GetUserBodyMetricsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<BodyMetricResponse> CreateBodyMetricAsync(int userId, CreateBodyMetricRequest request);
    Task<BodyMetricResponse?> UpdateBodyMetricAsync(int userId, int bodyMetricId, UpdateBodyMetricRequest request);
    Task<bool> DeleteBodyMetricAsync(int userId, int bodyMetricId);
}

public class BodyMetricService : IBodyMetricService
{
    private readonly ApplicationDbContext _context;

    public BodyMetricService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BodyMetricResponse>> GetUserBodyMetricsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.BodyMetrics.Where(b => b.UserId == userId && b.Status != 0);

        if (startDate.HasValue)
            query = query.Where(b => b.MeasuredDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(b => b.MeasuredDate <= endDate.Value);

        return await query
            .OrderByDescending(b => b.MeasuredDate)
            .Select(b => new BodyMetricResponse
            {
                Id = b.Id,
                UserId = b.UserId,
                MeasuredDate = b.MeasuredDate,
                WeightKg = b.WeightKg,
                HeightCm = b.HeightCm,
                BodyFatPercent = b.BodyFatPercent,
                Notes = b.Notes,
                Status = b.Status,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<BodyMetricResponse> CreateBodyMetricAsync(int userId, CreateBodyMetricRequest request)
    {
        var bodyMetric = new BodyMetric
        {
            UserId = userId,
            MeasuredDate = request.MeasuredDate,
            WeightKg = request.WeightKg,
            HeightCm = request.HeightCm,
            BodyFatPercent = request.BodyFatPercent,
            Notes = request.Notes,
            Status = 1
        };

        _context.BodyMetrics.Add(bodyMetric);
        await _context.SaveChangesAsync();

        return new BodyMetricResponse
        {
            Id = bodyMetric.Id,
            UserId = bodyMetric.UserId,
            MeasuredDate = bodyMetric.MeasuredDate,
            WeightKg = bodyMetric.WeightKg,
            HeightCm = bodyMetric.HeightCm,
            BodyFatPercent = bodyMetric.BodyFatPercent,
            Notes = bodyMetric.Notes,
            Status = bodyMetric.Status,
            CreatedAt = bodyMetric.CreatedAt
        };
    }

    public async Task<BodyMetricResponse?> UpdateBodyMetricAsync(int userId, int bodyMetricId, UpdateBodyMetricRequest request)
    {
        var bodyMetric = await _context.BodyMetrics.FirstOrDefaultAsync(b => b.Id == bodyMetricId && b.Status != 0);

        if (bodyMetric == null)
            return null;

        if (bodyMetric.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this body metric record");

        if (request.MeasuredDate.HasValue) bodyMetric.MeasuredDate = request.MeasuredDate.Value;
        if (request.WeightKg.HasValue) bodyMetric.WeightKg = request.WeightKg.Value;
        if (request.HeightCm.HasValue) bodyMetric.HeightCm = request.HeightCm.Value;
        if (request.BodyFatPercent.HasValue) bodyMetric.BodyFatPercent = request.BodyFatPercent.Value;
        if (request.Notes != null) bodyMetric.Notes = request.Notes;
        if (request.Status.HasValue) bodyMetric.Status = request.Status.Value;

        await _context.SaveChangesAsync();

        return new BodyMetricResponse
        {
            Id = bodyMetric.Id,
            UserId = bodyMetric.UserId,
            MeasuredDate = bodyMetric.MeasuredDate,
            WeightKg = bodyMetric.WeightKg,
            HeightCm = bodyMetric.HeightCm,
            BodyFatPercent = bodyMetric.BodyFatPercent,
            Notes = bodyMetric.Notes,
            Status = bodyMetric.Status,
            CreatedAt = bodyMetric.CreatedAt
        };
    }

    public async Task<bool> DeleteBodyMetricAsync(int userId, int bodyMetricId)
    {
        var bodyMetric = await _context.BodyMetrics.FirstOrDefaultAsync(b => b.Id == bodyMetricId && b.Status != 0);

        if (bodyMetric == null)
            return false;

        if (bodyMetric.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this body metric record");

        bodyMetric.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
