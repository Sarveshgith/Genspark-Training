using NotificationApp.Models;

namespace NotificationApp.Interfaces;

internal interface INotificationRepository : IRepository<int, Notification>
{
    List<Notification> GetByUserId(int userId);
}
