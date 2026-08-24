using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

// User Response DTO
public class UserProfileResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public byte Plan { get; set; } = 0;
}

// User Update DTO
public class UpdateUserRequest
{
    [MaxLength(100)]
    public string? Username { get; set; }

    [MaxLength(15)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(1024)]
    public string? Password { get; set; }
}

public class DeleteUserResponse
{
    public string Message { get; set; } = string.Empty;
}
