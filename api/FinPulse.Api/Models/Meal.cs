using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("meals", Schema = "body")]
public class Meal
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("meal_date")]
    public DateTime MealDate { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("meal_type")]
    public string MealType { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("calories", TypeName = "decimal(8,2)")]
    public decimal Calories { get; set; }

    [Column("protein_grams", TypeName = "decimal(6,2)")]
    public decimal? ProteinGrams { get; set; }

    [Column("carbs_grams", TypeName = "decimal(6,2)")]
    public decimal? CarbsGrams { get; set; }

    [Column("fat_grams", TypeName = "decimal(6,2)")]
    public decimal? FatGrams { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public short Status { get; set; } = 1;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
