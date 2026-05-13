using System;
using GuessGameApp.Exceptions;
using GuessGameApp.Helpers;
using GuessGameApp.Services;
using GuessGameApp.Models;

namespace GuessGameApp.Main;

//Main game loop and logic aggregations class.
internal class GuessGame
{
    private readonly WordProvider wordProvider;
    private readonly GuessValidator guessValidator;
    private readonly FeedbackGenerator feedbackGenerator;
    private readonly ConsoleCommenter consoleCommenter;
    private readonly ScoreCalculator scoreCalculator;
    private readonly Repositories.GameSessionRepository gameSessionRepository;

    private readonly HashSet<string> previousGuesses;
    private readonly Models.User currentUser;

    public GuessGame(User user)
    {
        currentUser = user;
        wordProvider = new WordProvider();
        guessValidator = new GuessValidator();
        feedbackGenerator = new FeedbackGenerator();
        consoleCommenter = new ConsoleCommenter();
        scoreCalculator = new ScoreCalculator();
        gameSessionRepository = new Repositories.GameSessionRepository();

        previousGuesses = new HashSet<string>();
    }

    public void Start()
    {
        Console.Write("Select difficulty - (E)asy, (M)edium, (H)ard [E]: ");
        string diffInput = (Console.ReadLine() ?? string.Empty).Trim().ToUpper();
        var difficulty = WordProvider.Difficulty.Easy;
        if (diffInput.StartsWith("M")) difficulty = WordProvider.Difficulty.Medium;
        else if (diffInput.StartsWith("H")) difficulty = WordProvider.Difficulty.Hard;

        string hiddenWord = wordProvider.GetRandomWord(difficulty);
        string targetWord = hiddenWord.ToUpper();

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

                    int score = scoreCalculator.CalculateScore(attempt);

                    var gameModel = new Game
                    {
                        userId = currentUser.id,
                        difficultyLvl = difficulty.ToString(),
                        isWin = true,
                        attempts = attempt,
                        score = score,
                        createdAt = DateTime.Now
                    };

                    gameSessionRepository.SaveGameSession(gameModel);

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

        consoleCommenter.PrintLoseComment(hiddenWord);

        var loseModel = new Game
        {
            userId = currentUser.id,
            difficultyLvl = difficulty.ToString(),
            isWin = false,
            attempts = maxAttempts,
            score = 0,
            createdAt = DateTime.Now
        };

        gameSessionRepository.SaveGameSession(loseModel);
    }
}
