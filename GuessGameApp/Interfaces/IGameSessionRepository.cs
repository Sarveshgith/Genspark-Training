using System;
using GuessGameApp.Models;

namespace GuessGameApp.Interfaces;

public interface IGameSessionRepository
{
    void SaveGameSession(Game gameSession);
}
