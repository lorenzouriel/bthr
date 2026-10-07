using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bthr.Api.Models;

[Table("habits", Schema = "body")]
public class Habit
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("habit_name")]
    public string HabitName { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("category")]
    public string? Category { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("target_frequency")]
    public string TargetFrequency { get; set; } = "Daily";

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public short Status { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}
