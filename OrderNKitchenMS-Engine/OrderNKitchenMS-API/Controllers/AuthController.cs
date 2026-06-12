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
        _logger.LogInformation("Register requested for Email: {Email}", userRegisterDto.Email);
        var user = await _authService.RegisterAsync(userRegisterDto);
        _logger.LogInformation("Register succeeded for User: {Email} (ID: {Id})", user.Email, user.Id);
        return Ok(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserLoginResponseDto>> Login([FromBody] UserLoginDto userLoginDto)
    {
        _logger.LogInformation("Login requested for Email: {Email}", userLoginDto?.Email);
        var response = await _authService.LoginAsync(userLoginDto);
        _logger.LogInformation("Login succeeded for User: {Email} (ID: {Id})", response.User.Email, response.User.Id);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("guest-login")]
    public async Task<ActionResult<GuestLoginResponseDto>> GuestLogin(int tableId)
    {
        _logger.LogInformation("GuestLogin requested for TableId: {TableId}", tableId);
        var response = await _authService.GuestLoginAsync(tableId);
        _logger.LogInformation("GuestLogin succeeded for TableId: {TableId}", tableId);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<UserLoginResponseDto>> Refresh([FromBody] TokenRefreshDto tokenRefreshDto)
    {
        _logger.LogInformation("Token refresh requested");
        var response = await _authService.RefreshTokenAsync(tokenRefreshDto);
        _logger.LogInformation("Token refresh succeeded for User: {Email} (ID: {Id})", response.User.Email, response.User.Id);
        return Ok(response);
    }
}
