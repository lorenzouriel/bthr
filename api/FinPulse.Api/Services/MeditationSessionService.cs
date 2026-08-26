using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IMeditationSessionService
{
    Task<List<MeditationSessionResponse>> GetUserMeditationSessionsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<MeditationSessionResponse> CreateMeditationSessionAsync(int userId, CreateMeditationSessionRequest request);
    Task<MeditationSessionResponse?> UpdateMeditationSessionAsync(int userId, int sessionId, UpdateMeditationSessionRequest request);
    Task<bool> DeleteMeditationSessionAsync(int userId, int sessionId);
}

public class MeditationSessionService : IMeditationSessionService
{
    private readonly ApplicationDbContext _context;

    public MeditationSessionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MeditationSessionResponse>> GetUserMeditationSessionsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.MeditationSessions.Where(s => s.UserId == userId && s.Status != 0);

        if (startDate.HasValue)
            query = query.Where(s => s.SessionDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.SessionDate <= endDate.Value);

        return await query
            .OrderByDescending(s => s.SessionDate)
            .Select(s => new MeditationSessionResponse
            {
                Id = s.Id,
                UserId = s.UserId,
                SessionDate = s.SessionDate,
                DurationMinutes = s.DurationMinutes,
                MeditationType = s.MeditationType,
                MoodBefore = s.MoodBefore,
                MoodAfter = s.MoodAfter,
                Notes = s.Notes,
                Status = s.Status,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<MeditationSessionResponse> CreateMeditationSessionAsync(int userId, CreateMeditationSessionRequest request)
    {
        var session = new MeditationSession
        {
            UserId = userId,
            SessionDate = request.SessionDate,
            DurationMinutes = request.DurationMinutes,
            MeditationType = request.MeditationType,
            MoodBefore = request.MoodBefore,
            MoodAfter = request.MoodAfter,
            Notes = request.Notes,
            Status = 1
        };

        _context.MeditationSessions.Add(session);
        await _context.SaveChangesAsync();

        return new MeditationSessionResponse
        {
            Id = session.Id,
            UserId = session.UserId,
            SessionDate = session.SessionDate,
            DurationMinutes = session.DurationMinutes,
            MeditationType = session.MeditationType,
            MoodBefore = session.MoodBefore,
            MoodAfter = session.MoodAfter,
            Notes = session.Notes,
            Status = session.Status,
            CreatedAt = session.CreatedAt
        };
    }

    public async Task<MeditationSessionResponse?> UpdateMeditationSessionAsync(int userId, int sessionId, UpdateMeditationSessionRequest request)
    {
        var session = await _context.MeditationSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.Status != 0);

        if (session == null)
            return null;

        if (session.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this meditation session");

        if (request.SessionDate.HasValue) session.SessionDate = request.SessionDate.Value;
        if (request.DurationMinutes.HasValue) session.DurationMinutes = request.DurationMinutes.Value;
        if (request.MeditationType != null) session.MeditationType = request.MeditationType;
        if (request.MoodBefore.HasValue) session.MoodBefore = request.MoodBefore.Value;
        if (request.MoodAfter.HasValue) session.MoodAfter = request.MoodAfter.Value;
        if (request.Notes != null) session.Notes = request.Notes;
        if (request.Status.HasValue) session.Status = request.Status.Value;

        await _context.SaveChangesAsync();

        return new MeditationSessionResponse
        {
            Id = session.Id,
            UserId = session.UserId,
            SessionDate = session.SessionDate,
            DurationMinutes = session.DurationMinutes,
            MeditationType = session.MeditationType,
            MoodBefore = session.MoodBefore,
            MoodAfter = session.MoodAfter,
            Notes = session.Notes,
            Status = session.Status,
            CreatedAt = session.CreatedAt
        };
    }

    public async Task<bool> DeleteMeditationSessionAsync(int userId, int sessionId)
    {
        var session = await _context.MeditationSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.Status != 0);

        if (session == null)
            return false;

        if (session.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this meditation session");

        session.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
