using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IUserService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<List<UserProfileResponse>> GetAllUsersAsync();
    Task<UserProfileResponse?> GetUserByIdAsync(int id);
    Task<UserProfileResponse?> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(int id);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<bool> IsUserAdminAsync(int userId);
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;

    public UserService(ApplicationDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        // Check for existing email
        if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Status != 0))
        {
            throw new InvalidOperationException("Email already registered");
        }

        // Check for existing username
        if (await _context.Users.AnyAsync(u => u.Username == request.Username && u.Status != 0))
        {
            throw new InvalidOperationException("Username already taken");
        }

        // Check for existing phone number
        if (await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber && u.Status != 0))
        {
            throw new InvalidOperationException("Phone number already registered");
        }

        var user = new User
        {
            Username = request.Username,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = 1
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new RegisterResponse
        {
            UserId = user.Id
        };
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.Status != 0);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            return null;
        }

        var token = _jwtService.GenerateToken(user.Id, user.Plan, isAdmin: user.Status == 3);

        return new LoginResponse
        {
            AccessToken = token,
            UserId = user.Id
        };
    }

    public async Task<List<UserProfileResponse>> GetAllUsersAsync()
    {
        return await _context.Users
            .Where(u => u.Status != 0)
            .Select(u => new UserProfileResponse
            {
                Id = u.Id,
                Username = u.Username,
                PhoneNumber = u.PhoneNumber,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                Plan = u.Plan
            })
            .ToListAsync();
    }

    public async Task<UserProfileResponse?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.Status != 0);

        if (user == null)
            return null;

        return new UserProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            Plan = user.Plan
        };
    }

    public async Task<UserProfileResponse?> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.Status != 0);

        if (user == null)
            return null;

        // Check for unique constraints if updating
        if (request.Email != null && request.Email != user.Email)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id && u.Status != 0))
            {
                throw new InvalidOperationException("Email already in use");
            }
            user.Email = request.Email;
        }

        if (request.Username != null && request.Username != user.Username)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username && u.Id != id && u.Status != 0))
            {
                throw new InvalidOperationException("Username already in use");
            }
            user.Username = request.Username;
        }

        if (request.PhoneNumber != null && request.PhoneNumber != user.PhoneNumber)
        {
            if (await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber && u.Id != id && u.Status != 0))
            {
                throw new InvalidOperationException("Phone number already in use");
            }
            user.PhoneNumber = request.PhoneNumber;
        }

        if (request.Password != null)
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await _context.SaveChangesAsync();

        return new UserProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            Plan = user.Plan
        };
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.Status != 0);

        if (user == null)
            return false;

        user.Status = 0; // Soft delete
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.Status != 0);

        if (user == null)
            return false;

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
            throw new InvalidOperationException("Senha atual incorreta");

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> IsUserAdminAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Status != 0);
        return user?.Status == 3;
    }
}
