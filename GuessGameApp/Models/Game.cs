using System;

namespace GuessGameApp.Models;

public class Game
{
    public int userId {get; set;}
    public string difficultyLvl { get; set; } = string.Empty;
    public bool isWin { get; set; }
    public int attempts { get; set; }
    public int score { get; set; }
    public DateTime createdAt { get; set; } = DateTime.Now;
}
