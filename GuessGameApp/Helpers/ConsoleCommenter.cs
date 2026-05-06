using System;
using GuessGameApp.Services;

namespace GuessGameApp.Helpers;

//This class handles all the console output for the game
internal class ConsoleCommenter
{
    ScoreCalculator scoreCalculator = new ScoreCalculator();

    private void PrintComment(int attempt)
    {
        switch (attempt)
        {
            case 1:
                Console.WriteLine("\nGenius!");
                break;

            case 2:
                Console.WriteLine("\nExcellent!");
                break;

            case 3:
                Console.WriteLine("\nGreat job!");
                break;

            case 4:
                Console.WriteLine("\nGood work!");
                break;

            case 5:
                Console.WriteLine("\nNice try!");
                break;

            case 6:
                Console.WriteLine("\nThat was close!");
                break;
            
            default:
                Console.WriteLine("\nBetter luck next time!");
                break;
        }
    }

    public void PrintWinComment(int attempt)
    {
        int score = scoreCalculator.CalculateScore(attempt);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nCongratulations! You've guessed the word!");
        Console.WriteLine($"Your score: {score}");
        PrintComment(attempt);
        Console.ResetColor();
    }

    public void PrintLoseComment(string hiddenWord)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\nGame Over! The word was: {hiddenWord}");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Red;
        PrintComment(0);
        Console.ResetColor();
    }

    public void DisplayFeedback(string guess, char[] feedback)
    {
        for(int i = 0; i < guess.Length; i++)
        {
            if(feedback[i] == 'G')
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(guess[i]);
            }
            else if(feedback[i] == 'Y')
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(guess[i]);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(guess[i]);
            }
        }
        Console.ResetColor();
        Console.WriteLine();
    }
}
