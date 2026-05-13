using System;
using GuessGameApp.Models;

namespace GuessGameApp.Interfaces;

public interface IUserRepository
{
    void RegisterUser(User user);

    User? LoginUser(string username, string password);
}
