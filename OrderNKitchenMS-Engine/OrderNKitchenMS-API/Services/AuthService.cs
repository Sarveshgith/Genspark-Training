using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Logging;

namespace OrderNKitchenMS_API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository userRepository, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    // Registers a new user with validation and email uniqueness check.
    public async Task<UserDto> RegisterAsync(UserRegisterDto userRegisterDto)
    {
        _logger.LogInformation("Registering new user with Email: {Email}", userRegisterDto.Email);
        ValidateRegisterDto(userRegisterDto);

        var user = await _userRepository.GetByEmailAsync(userRegisterDto.Email);
        if (user != null)
        {
            _logger.LogWarning("Registration failed: Email {Email} is already registered.", userRegisterDto.Email);
            throw new ConflictException("Email is already registered.");
        }

        var roles = await _userRepository.GetAllRolesAsync();
        if (!roles.Any(r => r.Id == userRegisterDto.RoleId))
        {
            _logger.LogWarning("Registration failed: Role ID {RoleId} does not exist.", userRegisterDto.RoleId);
            throw new NotFoundException($"Role with ID {userRegisterDto.RoleId} does not exist.");
        }

        var newUser = new User
        {
            Name = userRegisterDto.Name,
            Email = userRegisterDto.Email,
            PasswordHash = _tokenService.HashPassword(userRegisterDto.Password),
            PhoneNumber = userRegisterDto.PhoneNumber,
            Address = userRegisterDto.Address,
            RoleId = userRegisterDto.RoleId
        };

        var createdUser = await _userRepository.CreateAsync(newUser);
        _logger.LogInformation("Registration successful. Created User ID: {Id} for Email: {Email}", createdUser.Id, createdUser.Email);
        return MapUserToDto(createdUser);
    }

    // Authenticates a user and returns a login response with a JWT token.
    public async Task<UserLoginResponseDto> LoginAsync(UserLoginDto userLoginDto)
    {
        _logger.LogInformation("Logging in user with Email: {Email}", userLoginDto.Email);
        Validation.RequireNotNull(userLoginDto, nameof(userLoginDto), "Login data is required.");
        Validation.RequireValidEmail(userLoginDto.Email, nameof(userLoginDto.Email));
        Validation.RequireNonEmptyString(userLoginDto.Password, nameof(userLoginDto.Password), "Password is required.");

        var user = await _userRepository.GetByEmailAsync(userLoginDto.Email);

        if (user == null || !_tokenService.VerifyPassword(userLoginDto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid credentials for Email: {Email}", userLoginDto.Email);
            throw new ArgumentException("Invalid email or password.");
        }

        var token = _tokenService.CreateJwtToken(user);
        var refreshToken = _tokenService.CreateRefreshJwtToken(user);

        _logger.LogInformation("Login successful. Generated JWT and Refresh Token for User ID: {Id}", user.Id);
        return new UserLoginResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            User = MapUserToDto(user)
        };
    }

    // Validates a refresh token and generates a new pair of access/refresh tokens.
    public async Task<UserLoginResponseDto> RefreshTokenAsync(TokenRefreshDto tokenRefreshDto)
    {
        _logger.LogInformation("Refreshing tokens");
        Validation.RequireNotNull(tokenRefreshDto, nameof(tokenRefreshDto), "Refresh token data is required.");
        Validation.RequireNonEmptyString(tokenRefreshDto.RefreshToken, nameof(tokenRefreshDto.RefreshToken), "Refresh token is required.");

        var principal = _tokenService.ValidateRefreshToken(tokenRefreshDto.RefreshToken);
        var userId = principal.GetUserId();
        _logger.LogInformation("Extracted UserId: {UserId} from refresh token", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Refresh failed: User with ID {UserId} was not found.", userId);
            throw new UnauthorizedException("User associated with this token was not found.");
        }

        var token = _tokenService.CreateJwtToken(user);
        var newRefreshToken = _tokenService.CreateRefreshJwtToken(user);

        _logger.LogInformation("Refresh successful. Generated new JWT and Refresh Token for User ID: {Id}", user.Id);
        return new UserLoginResponseDto
        {
            Token = token,
            RefreshToken = newRefreshToken,
            User = MapUserToDto(user)
        };
    }

    // Generates a guest token for a user based on the provided table ID.
    public async Task<GuestLoginResponseDto> GuestLoginAsync(int tableId)
    {
        _logger.LogInformation("Logging in as Guest for TableId: {TableId}", tableId);
        Validation.RequireGreaterThanZero(tableId, nameof(tableId), "Table ID must be greater than zero.");

        var token = _tokenService.CreateGuestToken(tableId);

        _logger.LogInformation("Guest login successful for TableId: {TableId}. Generated guest token.", tableId);
        return new GuestLoginResponseDto
        {
            Token = token
        };
    }

    private static void ValidateRegisterDto(UserRegisterDto userRegisterDto)
    {
        Validation.RequireNotNull(userRegisterDto, nameof(userRegisterDto), "User data is required.");
        Validation.RequireNonEmptyString(userRegisterDto.Name, nameof(userRegisterDto.Name), "Name is required.");
        Validation.RequireValidEmail(userRegisterDto.Email, nameof(userRegisterDto.Email));
        Validation.RequireGreaterThanZero(userRegisterDto.RoleId, nameof(userRegisterDto.RoleId), "Role is required.");
        Validation.RequireStrongPassword(userRegisterDto.Password, nameof(userRegisterDto.Password));
        Validation.RequireValidPhone(userRegisterDto.PhoneNumber, nameof(userRegisterDto.PhoneNumber));
        Validation.RequireNotEmptyIfProvided(userRegisterDto.Address, nameof(userRegisterDto.Address), "If provided, address cannot be empty.");
    }

    private static UserDto MapUserToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            RoleName = user.Role?.Name.ToString() ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            IsDeleted = user.IsDeleted.ToString()
        };
    }
}
