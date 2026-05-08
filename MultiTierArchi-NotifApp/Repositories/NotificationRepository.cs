using MultiTierArchi_NotifApp.Models;

namespace MultiTierArchi_NotifApp.Repositories;

internal class NotificationRepository : Repository<Notification>
{
    public override List<Notification> GetAll()
    {
        return _items.OrderByDescending(n => n.SentTime).ToList();
    }
}
