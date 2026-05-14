using NotificationApp.Contexts;
using NotificationApp.Interfaces;
using NotificationApp.Models;

namespace NotificationApp.Repository;

internal class NotificationRepository : INotificationRepository
{
    private readonly NotifContext _context;

    public NotificationRepository(NotifContext context = null)
    {
        _context = context ?? new NotifContext();
    }

    public Notification Create(Notification item)
    {
        _context.Notifications.Add(item);
        _context.SaveChanges();
        return item;
    }

    public Notification? Get(int id)
    {
        return _context.Notifications.FirstOrDefault(n => n.Id == id);
    }

    public List<Notification> GetAll()
    {
        return _context.Notifications
            .OrderByDescending(n => n.SentDate)
            .ToList();
    }

    public List<Notification> GetByUserId(int userId)
    {
        return _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.SentDate)
            .ToList();
    }

    public Notification? Update(int id, Notification item)
    {
        var existing = _context.Notifications.FirstOrDefault(n => n.Id == id);
        if (existing == null)
        {
            return null;
        }

        existing.UserId = item.UserId;
        existing.Message = item.Message;
        existing.NotifType = item.NotifType;
        existing.SentDate = item.SentDate;
        _context.SaveChanges();
        return existing;
    }

    public Notification? Delete(int id)
    {
        var notif = _context.Notifications.FirstOrDefault(n => n.Id == id);
        if (notif == null)
        {
            return null;
        }

        _context.Notifications.Remove(notif);
        _context.SaveChanges();
        return notif;
    }
}