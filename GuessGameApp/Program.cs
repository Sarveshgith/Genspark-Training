using System;
using GuessGameApp.Main;

bool playAgain = true;

while (playAgain)
{
    Game game = new Game();
    game.Start();

    Console.Write("\nDo you want to play again? (y/n): ");
    string response = Console.ReadLine()?.Trim().ToLower() ?? "n";
    playAgain = response == "y";
}

Console.WriteLine("Thanks for playing!");