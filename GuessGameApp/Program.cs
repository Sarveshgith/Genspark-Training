using System;
using GuessGameApp.Main;
using GuessGameApp.Repositories;
using GuessGameApp.Data;
using GuessGameApp.Models;

DatabaseInitializer.Initialize();

var userRepo = new UserRepository();

Console.WriteLine("Welcome to Guess Game (DB-backed)\n");

User? currentUser = null;

while (currentUser == null)
{
    Console.WriteLine("1) Login");
    Console.WriteLine("2) Register");
    Console.Write("Choose an option: ");

    string opt = Console.ReadLine()?.Trim() ?? string.Empty;
    if (opt == "1")
    {
        Console.Write("Username: ");
        var u = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.Write("Password: ");
        var p = Console.ReadLine()?.Trim() ?? string.Empty;

        var user = userRepo.LoginUser(u, p);
        if (user != null)
        {
            currentUser = user;
            Console.WriteLine($"Logged in as {currentUser.username}\n");
        }
        else
        {
            Console.WriteLine("Invalid credentials. Try again.\n");
        }
    }
    else if (opt == "2")
    {
        Console.Write("Enter username: ");
        var u = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.Write("Enter password: ");
        var p = Console.ReadLine()?.Trim() ?? string.Empty;

        var newUser = new User { username = u, password = p };
        userRepo.RegisterUser(newUser);
        Console.WriteLine("Registration complete. Please login.\n");
    }
    else
    {
        Console.WriteLine("Invalid option\n");
    }
}

bool playAgain = true;
while (playAgain)
{
    GuessGame game = new GuessGame(currentUser);
    game.Start();

    Console.Write("\nDo you want to play again? (y/n): ");
    string response = Console.ReadLine()?.Trim().ToLower() ?? "n";
    playAgain = response == "y";
}

Console.WriteLine("Thanks for playing!");