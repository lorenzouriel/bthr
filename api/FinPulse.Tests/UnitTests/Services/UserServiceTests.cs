using FluentAssertions;
using Moq;
using FinPulse.Api.Services;
using FinPulse.Api.DTOs;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Services;

public class UserServiceTests : ServiceTestBase
{
    private readonly UserService _sut;
    private readonly Mock<IJwtService> _jwtServiceMock;

    public UserServiceTests()
    {
        _jwtServiceMock = new Mock<IJwtService>();
        _sut = new UserService(Context, _jwtServiceMock.Object);
    }

    #region Register Tests

    [Fact]
    public async Task RegisterAsync_WithValidRequest_CreatesUserSuccessfully()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            PhoneNumber = "+1234567890",
            Password = "Test@123456"
        };

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().BeGreaterThan(0);

        var user = await Context.Users.FindAsync(result.UserId);
        user.Should().NotBeNull();
        user!.Email.Should().Be(request.Email);
        user.Username.Should().Be(request.Username);
        user.Status.Should().Be(1);
        BCrypt.Net.BCrypt.Verify(request.Password, user.Password).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingUser = new UserBuilder()
            .WithEmail("existing@example.com")
            .Build();
        await Context.Users.AddAsync(existingUser);
        await Context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "existing@example.com",
            PhoneNumber = "+9876543210",
            Password = "Test@123456"
        };

        // Act
        var act = async () => await _sut.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already registered");
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingUser = new UserBuilder()
            .WithUsername("existinguser")
            .Build();
        await Context.Users.AddAsync(existingUser);
        await Context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "existinguser",
            Email = "new@example.com",
            PhoneNumber = "+9876543210",
            Password = "Test@123456"
        };

        // Act
        var act = async () => await _sut.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Username already taken");
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicatePhoneNumber_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingUser = new UserBuilder()
            .WithPhoneNumber("+1234567890")
            .Build();
        await Context.Users.AddAsync(existingUser);
        await Context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "new@example.com",
            PhoneNumber = "+1234567890",
            Password = "Test@123456"
        };

        // Act
        var act = async () => await _sut.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Phone number already registered");
    }

    [Fact]
    public async Task RegisterAsync_IgnoresDeletedUsersWhenCheckingDuplicates()
    {
        // Arrange
        var deletedUser = new UserBuilder()
            .WithEmail("deleted@example.com")
            .AsDeleted()
            .Build();
        await Context.Users.AddAsync(deletedUser);
        await Context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "deleted@example.com",
            PhoneNumber = "+1234567890",
            Password = "Test@123456"
        };

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().BeGreaterThan(0);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUserId()
    {
        // Arrange
        var password = "Test@123456";
        var user = new UserBuilder()
            .WithEmail("login@example.com")
            .WithPassword(password)
            .Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();

        _jwtServiceMock.Setup(x => x.GenerateToken(user.Id))
            .Returns("fake-jwt-token");

        var request = new LoginRequest
        {
            Email = "login@example.com",
            Password = password
        };

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("fake-jwt-token");
        result.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@example.com")
            .WithPassword("CorrectPassword")
            .Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword"
        };

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword"
        };

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithDeletedUser_ReturnsNull()
    {
        // Arrange
        var deletedUser = new UserBuilder()
            .WithEmail("deleted@example.com")
            .WithPassword("Test@123456")
            .AsDeleted()
            .Build();
        await Context.Users.AddAsync(deletedUser);
        await Context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "deleted@example.com",
            Password = "Test@123456"
        };

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllUsers Tests

    [Fact]
    public async Task GetAllUsersAsync_ReturnsOnlyActiveUsers()
    {
        // Arrange
        var activeUsers = new[]
        {
            new UserBuilder().WithStatus(1).Build(),
            new UserBuilder().WithStatus(1).Build()
        };
        var deletedUser = new UserBuilder().AsDeleted().Build();

        await Context.Users.AddRangeAsync(activeUsers);
        await Context.Users.AddAsync(deletedUser);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllUsersAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().NotContain(u => u.Id == deletedUser.Id);
        result.Should().AllSatisfy(u =>
        {
            u.Username.Should().NotBeNullOrEmpty();
            u.Email.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetAllUsersAsync_WhenNoUsers_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAllUsersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetUserById Tests

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@example.com")
            .WithUsername("testuser")
            .Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("user@example.com");
        result.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetUserByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_WithDeletedUser_ReturnsNull()
    {
        // Arrange
        var deletedUser = new UserBuilder()
            .AsDeleted()
            .Build();
        await Context.Users.AddAsync(deletedUser);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserByIdAsync(deletedUser.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUserAsync_UpdatesEmailSuccessfully()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("old@example.com")
            .Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            Email = "new@example.com"
        };

        // Act
        var result = await _sut.UpdateUserAsync(user.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("new@example.com");

        var updatedUser = await Context.Users.FindAsync(user.Id);
        updatedUser!.Email.Should().Be("new@example.com");
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesPasswordSuccessfully()
    {
        // Arrange
        var user = new UserBuilder()
            .WithPassword("OldPassword")
            .Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        var oldPasswordHash = user.Password;

        var request = new UpdateUserRequest
        {
            Password = "NewPassword@123"
        };

        // Act
        var result = await _sut.UpdateUserAsync(user.Id, request);

        // Assert
        result.Should().NotBeNull();

        var updatedUser = await Context.Users.FindAsync(user.Id);
        updatedUser!.Password.Should().NotBe(oldPasswordHash);
        BCrypt.Net.BCrypt.Verify("NewPassword@123", updatedUser.Password).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var user1 = new UserBuilder().WithEmail("user1@example.com").Build();
        var user2 = new UserBuilder().WithEmail("user2@example.com").Build();
        await Context.Users.AddRangeAsync(user1, user2);
        await Context.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            Email = "user2@example.com"
        };

        // Act
        var act = async () => await _sut.UpdateUserAsync(user1.Id, request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already in use");
    }

    [Fact]
    public async Task UpdateUserAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            Email = "new@example.com"
        };

        // Act
        var result = await _sut.UpdateUserAsync(999, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserAsync_WithSameEmail_DoesNotThrow()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("user@example.com")
            .Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            Email = "user@example.com" // Same email
        };

        // Act
        var result = await _sut.UpdateUserAsync(user.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task UpdateUserAsync_WithDuplicateUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var user1 = new UserBuilder().WithUsername("user1").Build();
        var user2 = new UserBuilder().WithUsername("user2").Build();
        await Context.Users.AddRangeAsync(user1, user2);
        await Context.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            Username = "user2" // username already taken by user2
        };

        // Act
        var act = async () => await _sut.UpdateUserAsync(user1.Id, request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Username already in use");
    }

    [Fact]
    public async Task UpdateUserAsync_WithDuplicatePhoneNumber_ThrowsInvalidOperationException()
    {
        // Arrange
        var user1 = new UserBuilder().WithPhoneNumber("+11111111111").Build();
        var user2 = new UserBuilder().WithPhoneNumber("+12222222222").Build();
        await Context.Users.AddRangeAsync(user1, user2);
        await Context.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            PhoneNumber = "+12222222222" // phone already registered to user2
        };

        // Act
        var act = async () => await _sut.UpdateUserAsync(user1.Id, request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Phone number already in use");
    }

    #endregion

    #region DeleteUser Tests

    [Fact]
    public async Task DeleteUserAsync_SoftDeletesUser()
    {
        // Arrange
        var user = new UserBuilder().Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteUserAsync(user.Id);

        // Assert
        result.Should().BeTrue();

        var deletedUser = await Context.Users.FindAsync(user.Id);
        deletedUser!.Status.Should().Be(0);
    }

    [Fact]
    public async Task DeleteUserAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Act
        var result = await _sut.DeleteUserAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUserAsync_WithAlreadyDeletedUser_ReturnsFalse()
    {
        // Arrange
        var deletedUser = new UserBuilder()
            .AsDeleted()
            .Build();
        await Context.Users.AddAsync(deletedUser);
        await Context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteUserAsync(deletedUser.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
