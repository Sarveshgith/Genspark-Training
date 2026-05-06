using System;
using GuessGameApp.Exceptions;
using GuessGameApp.Helpers;
using GuessGameApp.Services;

namespace GuessGameApp.Main;

//Main game loop and logic aggregations class.
internal class Game
{
    private readonly WordProvider wordProvider;
    private readonly GuessValidator guessValidator;
    private readonly FeedbackGenerator feedbackGenerator;
    private readonly ConsoleCommenter consoleCommenter;

    private readonly HashSet<string> previousGuesses;

    public Game()
    {
        wordProvider = new WordProvider();
        guessValidator = new GuessValidator();
        feedbackGenerator = new FeedbackGenerator();
        consoleCommenter = new ConsoleCommenter();
        
        previousGuesses = new HashSet<string>();
    }

    public void Start()
    {
        string targetWord = wordProvider.GetRandomWord().ToUpper();

        int maxAttempts = 6;

        Console.WriteLine("====== WORD GUESS GAME ======\n");

        for(int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                Console.Write($"Attempt {attempt}/{maxAttempts} - Enter your guess: ");
                string guess = (Console.ReadLine() ?? string.Empty).Trim().ToUpper();

                guessValidator.validateGuess(guess);

                if (previousGuesses.Contains(guess))
                {
                    throw new InvalidGuessException("You have already guessed this word");
                }

                previousGuesses.Add(guess);

                char[] feedback = feedbackGenerator.GenerateFeedback(guess, targetWord);

                if(guess == targetWord)
                {
                    consoleCommenter.PrintWinComment(attempt);
                    return;
                }
                else
                {
                    consoleCommenter.DisplayFeedback(guess, feedback);
                }

            }
            catch(InvalidGuessException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Invalid guess: {ex.Message}");
                Console.ResetColor();
                attempt--;
            }
        }

        consoleCommenter.PrintLoseComment(targetWord);
    }
}
