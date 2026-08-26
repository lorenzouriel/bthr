using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("journal_entries", Schema = "mind")]
public class JournalEntry
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("entry_date")]
    public DateTime EntryDate { get; set; }

    [MaxLength(200)]
    [Column("title")]
    public string? Title { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("mood")]
    public short? Mood { get; set; }

    [MaxLength(50)]
    [Column("category")]
    public string? Category { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public short Status { get; set; } = 1;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
