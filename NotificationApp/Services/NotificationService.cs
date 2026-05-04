using NotificationApp.Interfaces;
using NotificationApp.Models;

namespace NotificationApp.Services;

internal class NotificationService
{
    private readonly List<User> users = new List<User>();
    private readonly List<Notification> notifications = new List<Notification>();

    public User CreateUser()
    {
        Console.WriteLine($"Enter user details:");
        Console.Write($"Name: ");
        string name = Console.ReadLine() ?? string.Empty;
        Console.Write($"Email: ");
        string email = Console.ReadLine() ?? string.Empty;
        Console.Write($"Phone number: ");
        string phoneNo = Console.ReadLine() ?? string.Empty;

        User user = new User(name, email, phoneNo);

        users.Add(user);
        return user;
    }

    public IReadOnlyList<User> GetUsers()
    {
        return users.AsReadOnly();
    }

    public void PrintUsers()
    {
        if (users.Count == 0)
        {
            Console.WriteLine("No users.");
            return;
        }

        foreach (var user in users)
        {
            PrintUser(user);
        }

        return;
    }

    public void SendNotification(INotification notificationSender, User user, Notification notification)
    {
        notificationSender.SendNotif(user, notification);
        notifications.Add(notification);
    }

    private void PrintUser(User user)
    {
        Console.WriteLine("-----------------------------");
        Console.WriteLine(user);
        Console.WriteLine("-----------------------------");
    }

}
