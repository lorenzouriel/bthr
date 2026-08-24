using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;
using FinPulse.Tests.Helpers.Builders;

namespace FinPulse.Tests.UnitTests.Controllers;

public class UsersControllerTests : ServiceTestBase
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UsersController _sut;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _sut = new UsersController(_userServiceMock.Object, Context);
    }

    private void SetupControllerContext(int userId)
    {
        var claims = new System.Security.Claims.Claim[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())
        };

        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuth");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        _sut.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };
    }

    #region GetAllUsers Tests

    [Fact]
    public async Task GetAllUsers_WhenUserIsAdmin_Returns200OkWithUsers()
    {
        // Arrange
        var adminUser = new UserBuilder().WithId(1).WithStatus(3).Build();
        await Context.Users.AddAsync(adminUser);
        await Context.SaveChangesAsync();

        SetupControllerContext(userId: 1);

        var expectedUsers = new List<UserProfileResponse>
        {
            new UserProfileResponse { Id = 2, Username = "user1", Email = "user1@example.com" },
            new UserProfileResponse { Id = 3, Username = "user2", Email = "user2@example.com" }
        };

        _userServiceMock
            .Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _sut.GetAllUsers();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedUsers);
    }

    [Fact]
    public async Task GetAllUsers_WhenUserIsNotAdmin_Returns403Forbidden()
    {
        // Arrange
        var regularUser = new UserBuilder().WithId(1).WithStatus(1).Build();
        await Context.Users.AddAsync(regularUser);
        await Context.SaveChangesAsync();

        SetupControllerContext(userId: 1);

        // Act
        var result = await _sut.GetAllUsers();

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetAllUsers_WhenNoUsersExist_Returns200OkWithEmptyList()
    {
        // Arrange
        var adminUser = new UserBuilder().WithId(1).WithStatus(3).Build();
        await Context.Users.AddAsync(adminUser);
        await Context.SaveChangesAsync();

        SetupControllerContext(userId: 1);

        _userServiceMock
            .Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(new List<UserProfileResponse>());

        // Act
        var result = await _sut.GetAllUsers();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var users = okResult.Value.Should().BeOfType<List<UserProfileResponse>>().Subject;
        users.Should().BeEmpty();
    }

    #endregion

    #region GetUser Tests

    [Fact]
    public async Task GetUser_WhenUserIsAdminAndUserExists_Returns200OkWithUser()
    {
        // Arrange
        var adminUser = new UserBuilder().WithId(1).WithStatus(3).Build();
        await Context.Users.AddAsync(adminUser);
        await Context.SaveChangesAsync();

        SetupControllerContext(userId: 1);

        var expectedUser = new UserProfileResponse
        {
            Id = 2,
            Username = "testuser",
            Email = "test@example.com"
        };

        _userServiceMock
            .Setup(x => x.GetUserByIdAsync(2))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _sut.GetUser(2);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedUser);
    }

    [Fact]
    public async Task GetUser_WhenUserIsNotAdmin_Returns403Forbidden()
    {
        // Arrange
        var regularUser = new UserBuilder().WithId(1).WithStatus(1).Build();
        await Context.Users.AddAsync(regularUser);
        await Context.SaveChangesAsync();

        SetupControllerContext(userId: 1);

        // Act
        var result = await _sut.GetUser(2);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetUser_WhenUserDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var adminUser = new UserBuilder().WithId(1).WithStatus(3).Build();
        await Context.Users.AddAsync(adminUser);
        await Context.SaveChangesAsync();

        SetupControllerContext(userId: 1);

        _userServiceMock
            .Setup(x => x.GetUserByIdAsync(999))
            .ReturnsAsync((UserProfileResponse?)null);

        // Act
        var result = await _sut.GetUser(999);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUser_WhenUserIsAdminAndDataValid_Returns200OkWithUpdatedUser()
    {
        // Arrange
        var adminUser = new UserBuilder().WithId(1).WithStatus(3).Build();
        await Context.Users.AddAsync(adminUser);
        await Context.SaveChangesAsync();

        SetupControllerContext(userId: 1);

        var request = new UpdateUserRequest
        {
            Username = "updateduser",
            Email = "updated@example.com"
        };

        var expectedResponse = new UserProfileResponse
        {
            Id = 2,
            Username = "updateduser",
            Email = "updated@example.com"
        };

        _userServiceMock
            .Setup(x => x.UpdateUserAsync(2, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.UpdateUser(2, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UpdateUser_WhenUserIsNotAdmin_Returns403Forbidden()
    {
        // Arrange
        var regularUser = new UserBuilder().WithId(1).WithStatus(1).Build();
        await Context.Users.AddAsync(regularUser);
        await Context.SaveChangesAsync();

        SetupControllerContext(userId: 1);

        var request = new UpdateUserRequest
        {
            Username = "updateduser"
        };

        // Act
        var result = await _sut.UpdateUser(2, request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateUser_WhenUserDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var adminUser = new UserBuilder().WithId(1).WithStatus(3).Build();
        await Context.Users.AddAsync(adminUser);
        await Context.SaveChangesAsync();

        SetupControllerContext(userId: 1);

        var request = new UpdateUserRequest
        {
            Username = "updateduser"
        };

        _userServiceMock
            .Setup(x => x.UpdateUserAsync(999, request))
            .ReturnsAsync((UserProfileResponse?)null);

        // Act
        var result = await _sut.UpdateUser(999, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateUser_WithDuplicateEmail_Returns400BadRequest()
    {
        // Arrange
        var adminUser = new UserBuilder().WithId(1).WithStatus(3).Build();
        await Context.Users.AddAsync(adminUser);
        await Context.SaveChangesAsync();

        SetupControllerContext(userId: 1);

        var request = new UpdateUserRequest
        {
            Email = "duplicate@example.com"
        };

        _userServiceMock
            .Setup(x => x.UpdateUserAsync(2, request))
            .ThrowsAsync(new InvalidOperationException("Email already in use"));

        // Act
        var result = await _sut.UpdateUser(2, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region DeleteUser Tests

    [Fact]
    public async Task DeleteUser_WhenUserIsAdminAndUserExists_Returns200Ok()
    {
        // Arrange
        var adminUser = new UserBuilder().WithId(1).WithStatus(3).Build();
        await Context.Users.AddAsync(adminUser);
        await Context.SaveChangesAsync();

        SetupControllerContext(userId: 1);

        _userServiceMock
            .Setup(x => x.DeleteUserAsync(2))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteUser(2);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<DeleteUserResponse>().Subject;
        response.Message.Should().Be("User deleted successfully");
    }

    [Fact]
    public async Task DeleteUser_WhenUserIsNotAdmin_Returns403Forbidden()
    {
        // Arrange
        var regularUser = new UserBuilder().WithId(1).WithStatus(1).Build();
        await Context.Users.AddAsync(regularUser);
        await Context.SaveChangesAsync();

        SetupControllerContext(userId: 1);

        // Act
        var result = await _sut.DeleteUser(2);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteUser_WhenUserDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var adminUser = new UserBuilder().WithId(1).WithStatus(3).Build();
        await Context.Users.AddAsync(adminUser);
        await Context.SaveChangesAsync();

        SetupControllerContext(userId: 1);

        _userServiceMock
            .Setup(x => x.DeleteUserAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.DeleteUser(999);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    #endregion
}
