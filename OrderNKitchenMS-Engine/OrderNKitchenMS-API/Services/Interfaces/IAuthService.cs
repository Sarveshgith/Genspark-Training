using System;
using OrderNKitchenMS_API.Models.DTOs;

namespace OrderNKitchenMS_API.Services.Interfaces;

public interface IAuthService
{
	public Task<UserDto> RegisterAsync(UserRegisterDto userRegisterDto);
	public Task<UserLoginResponseDto> LoginAsync(UserLoginDto userLoginDto);
	public Task<UserLoginResponseDto> RefreshTokenAsync(TokenRefreshDto tokenRefreshDto);
	public Task<GuestLoginResponseDto> GuestLoginAsync(string secret);
}
