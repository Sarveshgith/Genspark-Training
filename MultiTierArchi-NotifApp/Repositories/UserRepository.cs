using System.Collections.Generic;
using System.Linq;
using MultiTierArchi_NotifApp.Models;

namespace MultiTierArchi_NotifApp.Repositories;

internal class UserRepository : Repository<User>
{
    public override List<User> GetAll()
    {
        return _items.OrderBy(u => u.Email)
            .ThenBy(u => u.Name).ToList();
    }
}
