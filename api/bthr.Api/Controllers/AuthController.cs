using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using bthr.Api.DTOs;
using bthr.Api.Services;

namespace bthr.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly bool _secureCookie;

    public AuthController(
        IUserService userService,
        IJwtService jwtService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userService = userService;
        _jwtService = jwtService;
        _configuration = configuration;
        _logger = logger;
        _secureCookie = configuration.GetValue<bool>("Cookie:Secure", true);
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset? expires = null) => new CookieOptions
    {
        HttpOnly = true,
        Secure = _secureCookie,
        SameSite = _secureCookie ? SameSiteMode.None : SameSiteMode.Lax,
        Expires = expires
    };

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _userService.RegisterAsync(request);

            var token = _jwtService.GenerateToken(result.UserId);
            Response.Cookies.Append("access_token", token, BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(7)));

            return StatusCode(201, new { userId = result.UserId, token });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _userService.LoginAsync(request);

        if (result == null)
        {
            _logger.LogWarning("Failed login attempt");
            return Unauthorized(new { message = "Invalid credentials" });
        }

        Response.Cookies.Append("access_token", result.AccessToken, BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(7)));

        return Ok(new { userId = result.UserId, token = result.AccessToken });
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token", BuildCookieOptions());
        return Ok(new LogoutResponse { Message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(new { id = user.Id, email = user.Email, username = user.Username, isAdmin = User.FindFirst("admin")?.Value == "true" });
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var success = await _userService.ChangePasswordAsync(userId, request);
            if (!success) return Unauthorized();

            _logger.LogInformation("Password changed");

            return Ok(new { message = "Senha alterada com sucesso" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

}
