using System;

namespace GuessGameApp.Services;

internal class ScoreCalculator
{
    public int CalculateScore(int attempts)
    {
        if (attempts <= 2)
        {
            return 100;
        }
        else if (attempts == 3)
        {
            return 80;
        }
        else if (attempts == 4)
        {
            return 60;
        }
        else if (attempts == 5)
        {
            return 40;
        }
        else if (attempts == 6)
        {
            return 20;
        }
        else
        {
            return 0;
        }
    }
}
