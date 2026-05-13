using System;

namespace GuessGameApp.Models;

public class User
{
    public int id { get; set; }
    public string username{get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
    public DateTime createdAt { get; set; } = DateTime.Now;
}
