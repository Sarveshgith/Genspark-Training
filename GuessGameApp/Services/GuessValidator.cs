using System;
using GuessGameApp.Exceptions;
using GuessGameApp.Helpers;

namespace GuessGameApp.Services;

//This class will validate the user's guesses
internal class GuessValidator
{
    public void validateGuess(string guess)
    {
        if (string.IsNullOrWhiteSpace(guess))
        {
            throw new InvalidGuessException("Guess cannot be empty");
        }
        if(guess.Length < 5)
        {
            throw new InvalidGuessException("Guess length must be 5 characters");
        }
        if(guess.Length > 5)
        {
            throw new InvalidGuessException("Guess length must be 5 characters");
        }
        if(RegexValidator.HasAnyDigits(guess))
        {
            throw new InvalidGuessException("Guess cannot contain digits");
        }
        if(RegexValidator.HasAnySpecialChars(guess))
        {
            throw new InvalidGuessException("Guess cannot contain special characters");
        }
    }
}
