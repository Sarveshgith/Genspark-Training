using System;
using System.Text.RegularExpressions;

namespace MultiTierArchi_NotifApp.Helpers;

internal static class Validation
{
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        var s = email.Trim();
        var emailPattern = new Regex("^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$", RegexOptions.Compiled);
        return emailPattern.IsMatch(s);
    }

    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;

        var s = phone.Trim();
        var phonePattern = new Regex("^[0-9]{10}$", RegexOptions.Compiled);
        return phonePattern.IsMatch(s);
    }

    public static bool IsNonEmptyName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name);
    }

    public static bool IsValidMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        return message.Trim().Length >= 5;
    }

    public static bool IsSupportedNotificationType(string? notifType)
    {
        if (string.IsNullOrWhiteSpace(notifType)) return false;

        return notifType.Trim().ToLower().Equals("email")
            || notifType.Trim().ToLower().Equals("sms");
    }
}
