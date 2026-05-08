using System;

namespace MultiTierArchi_NotifApp.Models;

internal class InvalidFormatException : Exception
{
    public InvalidFormatException(string message) : base(message)
    {
    }
}
