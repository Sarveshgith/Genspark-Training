using System;

namespace MultiTierArchi_NotifApp.Models.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {}
}
