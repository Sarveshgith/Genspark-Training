using System.Text.RegularExpressions;

namespace LibraryManagementApp.Utils;

internal static class ValidationHelper
{
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        string pattern =
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        return Regex.IsMatch(email, pattern);
    }

    public static bool IsValidPhone(string phoneNo)
    {
        if (string.IsNullOrWhiteSpace(phoneNo))
            return false;

        string pattern =
            @"^\d{10}$";

        return Regex.IsMatch(phoneNo, pattern);
    }

    public static bool IsValid(string details)
    {
        return !string.IsNullOrWhiteSpace(details);
    }

    public static bool IsValidISBN(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return false;

        // Example: 9780306406157 or 0306406152
        string pattern =
            @"^\d{10}(\d{3})?$";

        return Regex.IsMatch(isbn, pattern);
    }
}