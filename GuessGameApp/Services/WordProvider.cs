using System;

namespace GuessGameApp.Services;

//Chooses the word for guessing and provides it to game logic.
internal class WordProvider
{
    private readonly List<string> words = new List<string>
    {
        "apple", "grape", "peach", "berry",
        "melon", "mango", "lemon"

    };

    public string GetRandomWord()
    {
        int index = Random.Shared.Next(words.Count);
        return words[index];
    }
}
