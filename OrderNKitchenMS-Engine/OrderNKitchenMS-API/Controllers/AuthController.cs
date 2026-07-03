// @feature Backend API | Auth Endpoint | Handles JWT login, token validation, user registration, and password hashing logic.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] UserRegisterDto userRegisterDto)
    {
        if (userRegisterDto == null)
        {
            return BadRequest("User registration data is required.");
        }

        _logger.LogInformation("Register requested.");
        var user = await _authService.RegisterAsync(userRegisterDto);
        _logger.LogInformation("Register succeeded for User ID: {Id}", user?.Id);
        return Ok(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserLoginResponseDto>> Login([FromBody] UserLoginDto userLoginDto)
    {
        if (userLoginDto == null)
        {
            return BadRequest("Login credentials are required.");
        }

        _logger.LogInformation("Login requested.");
        var response = await _authService.LoginAsync(userLoginDto);
        _logger.LogInformation("Login succeeded for User ID: {Id}", response?.User?.Id);
        return Ok(response);
    }

    [HttpPost("guest-login")]
    public async Task<ActionResult<GuestLoginResponseDto>> GuestLogin([FromBody] GuestLoginRequestDto guestLoginDto)
    {
        if (guestLoginDto == null || string.IsNullOrWhiteSpace(guestLoginDto.Secret))
        {
            return BadRequest("Table secret is required.");
        }

        _logger.LogInformation("GuestLogin requested.");
        var response = await _authService.GuestLoginAsync(guestLoginDto.Secret);
        _logger.LogInformation("GuestLogin succeeded.");
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<UserLoginResponseDto>> Refresh([FromBody] TokenRefreshDto tokenRefreshDto)
    {
        if (tokenRefreshDto == null || string.IsNullOrWhiteSpace(tokenRefreshDto.RefreshToken))
        {
            return BadRequest("Refresh token is required.");
        }

        _logger.LogInformation("Token refresh requested.");
        var response = await _authService.RefreshTokenAsync(tokenRefreshDto);
        _logger.LogInformation("Token refresh succeeded for User ID: {Id}", response?.User?.Id);
        return Ok(response);
    }
}
