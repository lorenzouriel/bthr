using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FinPulse.Api.Controllers;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;
using FinPulse.Tests.Helpers;

namespace FinPulse.Tests.UnitTests.Controllers;

public class AuthControllerTests : ControllerTestBase
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _sut = new AuthController(_userServiceMock.Object);
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            PhoneNumber = "+1234567890",
            Password = "Test@123456"
        };

        var expectedResponse = new RegisterResponse
        {
            UserId = 1,
            Password = request.Password
        };

        _userServiceMock
            .Setup(x => x.RegisterAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Register(request);

        // Assert
        var createdResult = result.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "duplicate@example.com",
            PhoneNumber = "+1234567890",
            Password = "Test@123456"
        };

        _userServiceMock
            .Setup(x => x.RegisterAsync(request))
            .ThrowsAsync(new InvalidOperationException("Email already registered"));

        // Act
        var result = await _sut.Register(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "duplicateuser",
            Email = "test@example.com",
            PhoneNumber = "+1234567890",
            Password = "Test@123456"
        };

        _userServiceMock
            .Setup(x => x.RegisterAsync(request))
            .ThrowsAsync(new InvalidOperationException("Username already taken"));

        // Act
        var result = await _sut.Register(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Register_WithDuplicatePhoneNumber_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            PhoneNumber = "+1234567890",
            Password = "Test@123456"
        };

        _userServiceMock
            .Setup(x => x.RegisterAsync(request))
            .ThrowsAsync(new InvalidOperationException("Phone number already registered"));

        // Act
        var result = await _sut.Register(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_Returns200OkWithToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test@123456"
        };

        var expectedResponse = new LoginResponse
        {
            AccessToken = "fake-jwt-token",
            UserId = 1
        };

        _userServiceMock
            .Setup(x => x.LoginAsync(request))
            .ReturnsAsync(expectedResponse);

        // Setup HttpContext for Response.Cookies access
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _sut.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401Unauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        _userServiceMock
            .Setup(x => x.LoginAsync(request))
            .ReturnsAsync((LoginResponse?)null);

        // Act
        var result = await _sut.Login(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401Unauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword"
        };

        _userServiceMock
            .Setup(x => x.LoginAsync(request))
            .ReturnsAsync((LoginResponse?)null);

        // Act
        var result = await _sut.Login(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public void Logout_Returns200OkWithSuccessMessage()
    {
        // Arrange
        SetupControllerContext(_sut, userId: 1);

        // Act
        var result = _sut.Logout();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<LogoutResponse>().Subject;
        response.Message.Should().Be("Logged out successfully");
    }

    #endregion
}
