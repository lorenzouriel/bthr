using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

// Auth Request DTOs
public class RegisterRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [MaxLength(100)]
    [RegularExpression(
        @"^(?=.*\d)(?=.*[^a-zA-Z0-9]).+$",
        ErrorMessage = "Password must contain at least one digit and one special character.")]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

// Auth Response DTOs
public class RegisterResponse
{
    public int UserId { get; set; }
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int UserId { get; set; }
}

public class LogoutResponse
{
    public string Message { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [RegularExpression(
        @"^(?=.*\d)(?=.*[^a-zA-Z0-9]).+$",
        ErrorMessage = "Password must contain at least one digit and one special character.")]
    public string NewPassword { get; set; } = string.Empty;
}

public class BotTokenRequest
{
    [Required]
    public int UserId { get; set; }
}
