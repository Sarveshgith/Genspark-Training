using System;

namespace GuessGameApp.Exceptions;

//Custom exception for invalid guesses
internal class InvalidGuessException : Exception
{
    public InvalidGuessException(string message) : base(message)
    {}
}
