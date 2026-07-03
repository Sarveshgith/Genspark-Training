using System;

namespace OrderNKitchenMS_API.Models.DTOs;

public class UserDto
{
    public int Id {get; set;}
    public string Email {get; set;} = string.Empty;
    public string Name {get; set;} = string.Empty;
    public string RoleName {get; set;} = string.Empty;
    public string? PhoneNumber {get; set;}
    public string? Address {get; set;}
    public string IsDeleted {get; set;} = string.Empty;
    public bool IsPending {get; set;}
}

public class UserRegisterDto
{
    public string Email {get; set;} = string.Empty;
    public string Name {get; set;} = string.Empty;
    public string Password {get; set;} = string.Empty;
    public int RoleId {get; set;}
    public string? PhoneNumber {get; set;}
    public string? Address {get; set;}
}

public class UserLoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserLoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

public class UserUpdateDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
}

public class UserRoleUpdateDto
{
    public int RoleId { get; set; }
}

public class QueryUserDto
{
    public string? Search { get; set; }
    public int? RoleId { get; set; }
    public bool? IsPending { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class TokenRefreshDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}

public class GuestLoginRequestDto
{
    public string Secret { get; set; } = string.Empty;
}

public class GuestLoginResponseDto
{
    public string Token { get; set; } = string.Empty;
}