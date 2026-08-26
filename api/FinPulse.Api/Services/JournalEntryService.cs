using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IJournalEntryService
{
    Task<List<JournalEntryResponse>> GetUserJournalEntriesAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<JournalEntryResponse> CreateJournalEntryAsync(int userId, CreateJournalEntryRequest request);
    Task<JournalEntryResponse?> UpdateJournalEntryAsync(int userId, int entryId, UpdateJournalEntryRequest request);
    Task<bool> DeleteJournalEntryAsync(int userId, int entryId);
}

public class JournalEntryService : IJournalEntryService
{
    private readonly ApplicationDbContext _context;

    public JournalEntryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<JournalEntryResponse>> GetUserJournalEntriesAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.JournalEntries.Where(e => e.UserId == userId && e.Status != 0);

        if (startDate.HasValue)
            query = query.Where(e => e.EntryDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.EntryDate <= endDate.Value);

        return await query
            .OrderByDescending(e => e.EntryDate)
            .Select(e => new JournalEntryResponse
            {
                Id = e.Id,
                UserId = e.UserId,
                EntryDate = e.EntryDate,
                Title = e.Title,
                Content = e.Content,
                Mood = e.Mood,
                Category = e.Category,
                Status = e.Status,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<JournalEntryResponse> CreateJournalEntryAsync(int userId, CreateJournalEntryRequest request)
    {
        var entry = new JournalEntry
        {
            UserId = userId,
            EntryDate = request.EntryDate,
            Title = request.Title,
            Content = request.Content,
            Mood = request.Mood,
            Category = request.Category,
            Status = 1
        };

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();

        return new JournalEntryResponse
        {
            Id = entry.Id,
            UserId = entry.UserId,
            EntryDate = entry.EntryDate,
            Title = entry.Title,
            Content = entry.Content,
            Mood = entry.Mood,
            Category = entry.Category,
            Status = entry.Status,
            CreatedAt = entry.CreatedAt
        };
    }

    public async Task<JournalEntryResponse?> UpdateJournalEntryAsync(int userId, int entryId, UpdateJournalEntryRequest request)
    {
        var entry = await _context.JournalEntries.FirstOrDefaultAsync(e => e.Id == entryId && e.Status != 0);

        if (entry == null)
            return null;

        if (entry.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this journal entry");

        if (request.EntryDate.HasValue) entry.EntryDate = request.EntryDate.Value;
        if (request.Title != null) entry.Title = request.Title;
        if (request.Content != null) entry.Content = request.Content;
        if (request.Mood.HasValue) entry.Mood = request.Mood.Value;
        if (request.Category != null) entry.Category = request.Category;
        if (request.Status.HasValue) entry.Status = request.Status.Value;

        await _context.SaveChangesAsync();

        return new JournalEntryResponse
        {
            Id = entry.Id,
            UserId = entry.UserId,
            EntryDate = entry.EntryDate,
            Title = entry.Title,
            Content = entry.Content,
            Mood = entry.Mood,
            Category = entry.Category,
            Status = entry.Status,
            CreatedAt = entry.CreatedAt
        };
    }

    public async Task<bool> DeleteJournalEntryAsync(int userId, int entryId)
    {
        var entry = await _context.JournalEntries.FirstOrDefaultAsync(e => e.Id == entryId && e.Status != 0);

        if (entry == null)
            return false;

        if (entry.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this journal entry");

        entry.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
