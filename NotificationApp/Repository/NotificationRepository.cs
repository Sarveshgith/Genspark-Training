using NotificationApp.Models;
using NotificationApp.Interfaces;

namespace NotificationApp.Repository;

internal class NotificationRepository : Repository<DateTime, Notification>
{
    public override Notification Create(Notification item)
    {
        _items[item.SentTime] = item;
        return item;
    }
}