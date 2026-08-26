using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreateJournalEntryRequest
{
    [Required]
    public DateTime EntryDate { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public short? Mood { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }
}

public class UpdateJournalEntryRequest
{
    public DateTime? EntryDate { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    public string? Content { get; set; }

    public short? Mood { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }

    public short? Status { get; set; }
}

public class JournalEntryResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime EntryDate { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public short? Mood { get; set; }
    public string? Category { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
