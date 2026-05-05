using System;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace NotificationApp.Services;

//This class contains static methods for validating user input such as email, phone number, and name.
//Static => No need for having multiple instances of this class
internal static class Validation
{
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var s = email.Trim();
        var emailPattern = new Regex("^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$", RegexOptions.Compiled);
        return emailPattern.IsMatch(s);
    }

    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        var s = phone.Trim();

        var phonePattern = new Regex("^[0-9]{10}$", RegexOptions.Compiled);
        return phonePattern.IsMatch(s);
    }

    public static bool IsNonEmptyName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name);
    }
}
