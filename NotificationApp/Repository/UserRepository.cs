using NotificationApp.Models;
using NotificationApp.Interfaces;

namespace NotificationApp.Repository;

//UserRepository => Uses Email as Unique Key
internal class UserRepository : Repository<string, User>
{
    public override User Create(User item)
    {
        _items[item.Email] = item;
        return item;
    }
}