using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace OrderNKitchenMS_API.Utils;

public static class Validation
{
    private static readonly EmailAddressAttribute EmailValidator = new();
    private static readonly Regex PhonePattern = new(
        @"^(\+91[\s-]?)?[6-9]\d{9}$",
        RegexOptions.Compiled);
    private static readonly Regex PasswordPattern = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$",
        RegexOptions.Compiled);

    public static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && EmailValidator.IsValid(email.Trim());

    public static bool IsValidPhone(string phone) =>
        !string.IsNullOrWhiteSpace(phone) && PhonePattern.IsMatch(phone.Trim());

    public static bool IsStrongPassword(string password) =>
        !string.IsNullOrWhiteSpace(password) && password.Length >= 8 && PasswordPattern.IsMatch(password);

    public static bool IsNonEmptyString(string value) =>
        !string.IsNullOrWhiteSpace(value);

    public static void Require(bool condition, string message, string paramName)
    {
        if (!condition)
        {
            throw new ArgumentException(message, paramName);
        }
    }

    public static void RequireNotNull<T>(T? value, string paramName, string message = "Data is required.") where T : class
    {
        if (value == null)
        {
            throw new ArgumentException(message, paramName);
        }
    }

    public static void RequireNonEmptyString(string value, string paramName, string message = "Value is required.")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, paramName);
        }
    }

    public static void RequireValidEmail(string email, string paramName, string message = "A valid email is required.")
    {
        if (!IsValidEmail(email))
        {
            throw new ArgumentException(message, paramName);
        }
    }

    public static void RequireValidPhone(string? phone, string paramName, string message = "If provided, phone number must be valid.")
    {
        if (!string.IsNullOrWhiteSpace(phone) && !IsValidPhone(phone))
        {
            throw new ArgumentException(message, paramName);
        }
    }

    public static void RequireStrongPassword(string password, string paramName, string message = "Password does not meet strength requirements.")
    {
        if (!IsStrongPassword(password))
        {
            throw new ArgumentException(message, paramName);
        }
    }

    public static void RequireNotEmptyIfProvided(string? value, string paramName, string message = "If provided, value cannot be empty.")
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, paramName);
        }
    }

    public static void RequireGreaterThanZero(int value, string paramName, string message = "Value must be greater than zero.")
    {
        if (value < 1)
        {
            throw new ArgumentException(message, paramName);
        }
    }

    public static void ValidateId(int id, string paramName = "id", string message = "Invalid ID. Must be greater than zero.")
    {
        if (id <= 0)
            throw new ArgumentException(message, paramName);
    }

    public static void ValidateRequest<TDto>(int id, TDto? dto, string idParamName = "id", string dtoParamName = "dto") where TDto : class
    {
        ValidateId(id, idParamName, $"Invalid {idParamName}. Must be greater than zero.");
        RequireNotNull(dto, dtoParamName, $"{dtoParamName} payload is required.");
    }

    public static void RequireValidEnum<TEnum>(int value, string paramName, string message = "Invalid enum value.") where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
        {
            throw new ArgumentException(message, paramName);
        }
    }
}