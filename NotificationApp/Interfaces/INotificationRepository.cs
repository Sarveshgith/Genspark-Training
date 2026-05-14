using NotificationApp.Models;

namespace NotificationApp.Interfaces;

internal interface INotificationRepository : IRepository<DateTime, Notification>
{
    List<Notification> GetByUserId(int userId);
}
