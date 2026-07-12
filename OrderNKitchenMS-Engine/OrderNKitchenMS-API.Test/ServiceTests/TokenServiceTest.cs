using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NUnit.Framework;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Models.Enums;
using OrderNKitchenMS_API.Services;

namespace OrderNKitchenMS_API.Test.ServiceTests;

[TestFixture]
public class TokenServiceTest
{
    private IConfiguration _configuration = null!;
    private Mock<ILogger<TokenService>> _loggerMock = null!;
    private TokenService _tokenService = null!;

    [SetUp]
    public void SetUp()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "SuperLongSecretKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:SecretRefreshKey", "SuperLongSecretRefreshKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" },
            { "JwtSettings:ExpiresInMinutes", "30" },
            { "JwtSettings:RefreshExpiresInDays", "5" },
            { "JwtSettings:GuestExpiresInHours", "2" }
        };

        _configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        _loggerMock = new Mock<ILogger<TokenService>>();
        _tokenService = new TokenService(_configuration, _loggerMock.Object);
    }

    [Test]
    public void VerifyPassword_HashPassword_ShouldVerifyCorrectly()
    {
        var password = "Password@123";
        var hash = _tokenService.HashPassword(password);

        Assert.That(_tokenService.VerifyPassword(password, hash), Is.True);
        Assert.That(_tokenService.VerifyPassword("wrongPassword", hash), Is.False);
    }

    [Test]
    public void CreateJwtToken_ValidUser_GeneratesToken()
    {
        var user = new User
        {
            Id = 10,
            Name = "John Doe",
            Email = "john@example.com",
            PasswordHash = "hash",
            Role = new Role { Id = 5, Name = UserRole.Waiter }
        };

        var token = _tokenService.CreateJwtToken(user);
        Assert.That(token, Is.Not.Null.Or.Empty);
    }

    [Test]
    public void CreateRefreshJwtToken_ValidUser_GeneratesToken()
    {
        var user = new User
        {
            Id = 10,
            Name = "John Doe",
            Email = "john@example.com",
            PasswordHash = "hash",
            Role = new Role { Id = 3, Name = UserRole.Chef }
        };

        var token = _tokenService.CreateRefreshJwtToken(user);
        Assert.That(token, Is.Not.Null.Or.Empty);
    }

    [Test]
    public void CreateGuestToken_ValidTable_GeneratesToken()
    {
        var token = _tokenService.CreateGuestToken(5);
        Assert.That(token, Is.Not.Null.Or.Empty);
    }

    [Test]
    public void ValidateRefreshToken_ValidToken_ReturnsPrincipal()
    {
        var user = new User
        {
            Id = 15,
            Name = "Alice Admin",
            Email = "admin@example.com",
            PasswordHash = "hash",
            Role = new Role { Id = 1, Name = UserRole.Admin }
        };

        var token = _tokenService.CreateRefreshJwtToken(user);
        var principal = _tokenService.ValidateRefreshToken(token);

        Assert.That(principal, Is.Not.Null);
        var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Assert.That(idClaim, Is.EqualTo("15"));
    }

    [Test]
    public void ValidateRefreshToken_InvalidToken_ThrowsUnauthorizedException()
    {
        Assert.Throws<UnauthorizedException>(() => _tokenService.ValidateRefreshToken("invalid-token-string"));
    }

    [Test]
    public void ValidateRefreshToken_ExpiredToken_ThrowsUnauthorizedException()
    {
        // Arrange: Make custom fast-expiry settings
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "SuperLongSecretKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:SecretRefreshKey", "SuperLongSecretRefreshKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" },
            { "JwtSettings:RefreshExpiresInDays", "-1" } // Instantly expired
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);

        var user = new User
        {
            Id = 15,
            Name = "Alice Admin",
            Email = "admin@example.com",
            PasswordHash = "hash",
            Role = new Role { Id = 1, Name = UserRole.Admin }
        };

        var token = service.CreateRefreshJwtToken(user);

        // Act & Assert
        Assert.Throws<UnauthorizedException>(() => service.ValidateRefreshToken(token));
    }

    [Test]
    public void ValidateRefreshToken_InvalidAlgorithm_ThrowsUnauthorizedException()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "SuperLongSecretKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:SecretRefreshKey", "SuperLongSecretRefreshKeyForSigningSymmetricSecurityKey123!AndNeedToBeEvenLongerForSha384" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(settings["JwtSettings:SecretRefreshKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha384);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: new List<Claim> { new(ClaimTypes.NameIdentifier, "1") },
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds
        );
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        Assert.Throws<UnauthorizedException>(() => service.ValidateRefreshToken(jwt));
    }

    [Test]
    public void CreateJwtToken_MissingSecretKey_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var service = new TokenService(config, _loggerMock.Object);
        var user = new User { Id = 1, Name = "A", Email = "a@a.com", PasswordHash = "h" };
        Assert.Throws<InvalidOperationException>(() => service.CreateJwtToken(user));
    }

    [Test]
    public void CreateJwtToken_NullRoleAndInvalidExpires_UsesDefaults()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "SuperLongSecretKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" },
            { "JwtSettings:ExpiresInMinutes", "invalid" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        var user = new User { Id = 1, Name = "A", Email = "a@a.com", PasswordHash = "h", Role = null };

        var token = service.CreateJwtToken(user);
        Assert.That(token, Is.Not.Null.Or.Empty);
    }

    [Test]
    public void CreateJwtToken_MissingIssuer_ThrowsInvalidOperationException()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "SuperLongSecretKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Audience", "TestAudience" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        var user = new User { Id = 1, Name = "A", Email = "a@a.com", PasswordHash = "h" };
        Assert.Throws<InvalidOperationException>(() => service.CreateJwtToken(user));
    }

    [Test]
    public void CreateJwtToken_MissingAudience_ThrowsInvalidOperationException()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "SuperLongSecretKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Issuer", "TestIssuer" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        var user = new User { Id = 1, Name = "A", Email = "a@a.com", PasswordHash = "h" };
        Assert.Throws<InvalidOperationException>(() => service.CreateJwtToken(user));
    }

    [Test]
    public void CreateRefreshJwtToken_MissingSecretRefreshKey_ThrowsInvalidOperationException()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        var user = new User { Id = 1, Name = "A", Email = "a@a.com", PasswordHash = "h" };
        Assert.Throws<InvalidOperationException>(() => service.CreateRefreshJwtToken(user));
    }

    [Test]
    public void CreateRefreshJwtToken_MissingIssuer_ThrowsInvalidOperationException()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretRefreshKey", "SuperLongSecretRefreshKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Audience", "TestAudience" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        var user = new User { Id = 1, Name = "A", Email = "a@a.com", PasswordHash = "h" };
        Assert.Throws<InvalidOperationException>(() => service.CreateRefreshJwtToken(user));
    }

    [Test]
    public void CreateRefreshJwtToken_MissingAudience_ThrowsInvalidOperationException()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretRefreshKey", "SuperLongSecretRefreshKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Issuer", "TestIssuer" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        var user = new User { Id = 1, Name = "A", Email = "a@a.com", PasswordHash = "h" };
        Assert.Throws<InvalidOperationException>(() => service.CreateRefreshJwtToken(user));
    }

    [Test]
    public void CreateRefreshJwtToken_NullRoleAndInvalidExpires_UsesDefaults()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretRefreshKey", "SuperLongSecretRefreshKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" },
            { "JwtSettings:RefreshExpiresInDays", "invalid" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        var user = new User { Id = 1, Name = "A", Email = "a@a.com", PasswordHash = "h", Role = null };

        var token = service.CreateRefreshJwtToken(user);
        Assert.That(token, Is.Not.Null.Or.Empty);
    }

    [Test]
    public void CreateGuestToken_MissingSecretKey_ThrowsInvalidOperationException()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        Assert.Throws<InvalidOperationException>(() => service.CreateGuestToken(1));
    }

    [Test]
    public void CreateGuestToken_MissingIssuer_ThrowsInvalidOperationException()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "SuperLongSecretKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Audience", "TestAudience" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        Assert.Throws<InvalidOperationException>(() => service.CreateGuestToken(1));
    }

    [Test]
    public void CreateGuestToken_MissingAudience_ThrowsInvalidOperationException()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "SuperLongSecretKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Issuer", "TestIssuer" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        Assert.Throws<InvalidOperationException>(() => service.CreateGuestToken(1));
    }

    [Test]
    public void CreateGuestToken_InvalidExpires_UsesDefaults()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "SuperLongSecretKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" },
            { "JwtSettings:GuestExpiresInHours", "invalid" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);

        var token = service.CreateGuestToken(1);
        Assert.That(token, Is.Not.Null.Or.Empty);
    }

    [Test]
    public void ValidateRefreshToken_MissingSecretRefreshKey_ThrowsInvalidOperationException()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        Assert.Throws<InvalidOperationException>(() => service.ValidateRefreshToken("token"));
    }

    [Test]
    public void ValidateRefreshToken_MissingIssuer_ThrowsInvalidOperationException()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretRefreshKey", "SuperLongSecretRefreshKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Audience", "TestAudience" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        Assert.Throws<InvalidOperationException>(() => service.ValidateRefreshToken("token"));
    }

    [Test]
    public void ValidateRefreshToken_MissingAudience_ThrowsInvalidOperationException()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretRefreshKey", "SuperLongSecretRefreshKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Issuer", "TestIssuer" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        Assert.Throws<InvalidOperationException>(() => service.ValidateRefreshToken("token"));
    }

    [Test]
    public void CreateJwtToken_MissingExpiresInMinutes_UsesDefaults()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "SuperLongSecretKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        var user = new User { Id = 1, Name = "A", Email = "a@a.com", PasswordHash = "h", Role = null };

        var token = service.CreateJwtToken(user);
        Assert.That(token, Is.Not.Null.Or.Empty);
    }

    [Test]
    public void CreateRefreshJwtToken_MissingRefreshExpiresInDays_UsesDefaults()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretRefreshKey", "SuperLongSecretRefreshKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);
        var user = new User { Id = 1, Name = "A", Email = "a@a.com", PasswordHash = "h", Role = null };

        var token = service.CreateRefreshJwtToken(user);
        Assert.That(token, Is.Not.Null.Or.Empty);
    }

    [Test]
    public void CreateGuestToken_MissingGuestExpiresInHours_UsesDefaults()
    {
        var settings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "SuperLongSecretKeyForSigningSymmetricSecurityKey123!" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var service = new TokenService(config, _loggerMock.Object);

        var token = service.CreateGuestToken(1);
        Assert.That(token, Is.Not.Null.Or.Empty);
    }
}
