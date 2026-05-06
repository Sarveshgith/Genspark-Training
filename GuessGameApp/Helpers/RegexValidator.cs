using System;
using System.Text.RegularExpressions;

namespace GuessGameApp.Helpers;

internal static class RegexValidator
{
    //Check if the input does not contain any digits
    public static bool HasAnyDigits(string input)
    {
        return Regex.IsMatch(input, @"\d");
    }

    //Check if the input does not contain any special characters
    public static bool HasAnySpecialChars(string input)
    {
        return Regex.IsMatch(input, @"[^a-zA-Z0-9]");
    }
}
