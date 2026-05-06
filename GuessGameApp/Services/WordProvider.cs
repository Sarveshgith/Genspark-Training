using System;

namespace GuessGameApp.Services;

internal class WordProvider
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    private readonly List<string> easyWords = new()
    {
        "apple", "grape", "peach", "berry", "melon", "mango", "lemon"
    };

    private readonly List<string> mediumWords = new()
    {
        "olive", "cider", "prune", "basil", "spice", "cocoa", "papaw"
    };

    private readonly List<string> hardWords = new()
    {
        "fjord", "glyph", "nymph", "crypt", "vexed", "zesty", "brynd"
    };

    public string GetRandomWord(Difficulty difficulty)
    {
        List<string> selectedWords;

        if (difficulty == Difficulty.Easy)
        {
            selectedWords = easyWords;
        }
        else if (difficulty == Difficulty.Medium)
        {
            selectedWords = mediumWords;
        }
        else
        {
            selectedWords = hardWords;
        }

        int index = Random.Shared.Next(selectedWords.Count);

        return selectedWords[index];
    }
}