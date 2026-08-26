using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IPersonalRecordService
{
    Task<List<PersonalRecordResponse>> GetUserPersonalRecordsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<PersonalRecordResponse> CreatePersonalRecordAsync(int userId, CreatePersonalRecordRequest request);
}

public class PersonalRecordService : IPersonalRecordService
{
    private readonly ApplicationDbContext _context;

    public PersonalRecordService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PersonalRecordResponse>> GetUserPersonalRecordsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.PersonalRecords.Where(p => p.UserId == userId && p.Status != 0);

        if (startDate.HasValue)
            query = query.Where(p => p.AchievedDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(p => p.AchievedDate <= endDate.Value);

        return await query
            .OrderByDescending(p => p.AchievedDate)
            .Select(p => new PersonalRecordResponse
            {
                Id = p.Id,
                UserId = p.UserId,
                ExerciseName = p.ExerciseName,
                MetricType = p.MetricType,
                Value = p.Value,
                Unit = p.Unit,
                AchievedDate = p.AchievedDate,
                Notes = p.Notes,
                Status = p.Status,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<PersonalRecordResponse> CreatePersonalRecordAsync(int userId, CreatePersonalRecordRequest request)
    {
        var record = new PersonalRecord
        {
            UserId = userId,
            ExerciseName = request.ExerciseName,
            MetricType = request.MetricType,
            Value = request.Value,
            Unit = request.Unit,
            AchievedDate = request.AchievedDate,
            Notes = request.Notes,
            Status = 1
        };

        _context.PersonalRecords.Add(record);
        await _context.SaveChangesAsync();

        return new PersonalRecordResponse
        {
            Id = record.Id,
            UserId = record.UserId,
            ExerciseName = record.ExerciseName,
            MetricType = record.MetricType,
            Value = record.Value,
            Unit = record.Unit,
            AchievedDate = record.AchievedDate,
            Notes = record.Notes,
            Status = record.Status,
            CreatedAt = record.CreatedAt
        };
    }
}
