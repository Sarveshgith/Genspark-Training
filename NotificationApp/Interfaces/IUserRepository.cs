using NotificationApp.Models;

namespace NotificationApp.Interfaces;

internal interface IUserRepository : IRepository<int, User>
{
    User? GetByEmail(string email);
    User? GetByPhone(string phoneNo);
}
