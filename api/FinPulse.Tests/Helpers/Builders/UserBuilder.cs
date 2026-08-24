using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

/// <summary>
/// Fluent builder for creating User test data with realistic fake data.
/// </summary>
public class UserBuilder
{
    private readonly User _user;
    private static readonly Faker _faker = new Faker();

    public UserBuilder()
    {
        _user = new User
        {
            Id = 0, // Let EF Core auto-generate
            Username = _faker.Internet.UserName(),
            Email = _faker.Internet.Email(),
            PhoneNumber = _faker.Phone.PhoneNumber("+1##########"),
            Password = BCrypt.Net.BCrypt.HashPassword("Test@123456"),
            Status = 1, // Active
            CreatedAt = DateTime.UtcNow
        };
    }

    public UserBuilder WithId(int id)
    {
        _user.Id = id;
        return this;
    }

    public UserBuilder WithUsername(string username)
    {
        _user.Username = username;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _user.Email = email;
        return this;
    }

    public UserBuilder WithPhoneNumber(string phoneNumber)
    {
        _user.PhoneNumber = phoneNumber;
        return this;
    }

    public UserBuilder WithPassword(string password)
    {
        _user.Password = BCrypt.Net.BCrypt.HashPassword(password);
        return this;
    }

    public UserBuilder WithRawPassword(string password)
    {
        _user.Password = password;
        return this;
    }

    public UserBuilder WithStatus(byte status)
    {
        _user.Status = status;
        return this;
    }

    public UserBuilder AsActive()
    {
        _user.Status = 1;
        return this;
    }

    public UserBuilder AsDeleted()
    {
        _user.Status = 0;
        return this;
    }

    public UserBuilder AsAdmin()
    {
        _user.Status = 3;
        return this;
    }

    public UserBuilder WithCreatedAt(DateTime createdAt)
    {
        _user.CreatedAt = createdAt;
        return this;
    }

    public User Build() => _user;
}
