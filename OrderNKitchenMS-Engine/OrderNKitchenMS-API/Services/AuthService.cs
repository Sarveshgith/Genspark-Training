using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Models.Enums;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Logging;

namespace OrderNKitchenMS_API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ITableRepository _tableRepository;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository, 
        ITokenService tokenService, 
        ITableRepository tableRepository,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _tableRepository = tableRepository;
        _logger = logger;
    }

    // Registers a new user with validation and email uniqueness check.
    public async Task<UserDto> RegisterAsync(UserRegisterDto userRegisterDto)
    {
        _logger.LogInformation("Registering new user.");
        ValidateRegisterDto(userRegisterDto);

        var user = await _userRepository.GetByEmailAsync(userRegisterDto.Email);
        if (user != null)
        {
            _logger.LogWarning("Registration failed: Email is already registered.");
            throw new ConflictException("Email is already registered.");
        }

        var roles = await _userRepository.GetAllRolesAsync();
        var role = roles.FirstOrDefault(r => r.Id == userRegisterDto.RoleId);
        if (role == null)
        {
            _logger.LogWarning("Registration failed: Role ID {RoleId} does not exist.", userRegisterDto.RoleId);
            throw new NotFoundException($"Role with ID {userRegisterDto.RoleId} does not exist.");
        }

        if (role.Name != UserRole.Chef && role.Name != UserRole.Waiter)
        {
            _logger.LogWarning("Registration failed: Role {RoleName} is not allowed for registration.", role.Name);
            throw new BusinessRuleException("Only Chef and Waiter roles can be registered.");
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
        _logger.LogInformation("Registration successful. Created User ID: {Id}", createdUser.Id);
        return MapUserToDto(createdUser);
    }

    // Authenticates a user and returns a login response with a JWT token.
    public async Task<UserLoginResponseDto> LoginAsync(UserLoginDto userLoginDto)
    {
        Validation.RequireNotNull(userLoginDto, nameof(userLoginDto), "Login data is required.");
        Validation.RequireValidEmail(userLoginDto.Email, nameof(userLoginDto.Email));
        Validation.RequireNonEmptyString(userLoginDto.Password, nameof(userLoginDto.Password), "Password is required.");

        _logger.LogInformation("Attempting login.");
        var user = await _userRepository.GetByEmailAsync(userLoginDto.Email);

        if (user == null || !_tokenService.VerifyPassword(userLoginDto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid credentials.");
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (user.IsPending)
        {
            _logger.LogWarning("Login failed: User ID {Id} is pending approval.", user.Id);
            throw new ForbiddenException("Your account is pending admin approval.");
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

    // Generates a guest token for a user based on the provided table secret.
    public async Task<GuestLoginResponseDto> GuestLoginAsync(string secret)
    {
        _logger.LogInformation("Logging in as Guest with secret");
        Validation.RequireNonEmptyString(secret, nameof(secret), "Secret is required.");

        var table = await _tableRepository.GetBySecretAsync(secret);
        if (table == null)
        {
            _logger.LogWarning("Guest login failed: Invalid or missing table secret.");
            throw new UnauthorizedException("Invalid table secret.");
        }

        var token = _tokenService.CreateGuestToken(table.Id);

        _logger.LogInformation("Guest login successful for TableId: {TableId}. Generated guest token.", table.Id);
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
            IsDeleted = user.IsDeleted.ToString(),
            IsPending = user.IsPending
        };
    }
}
