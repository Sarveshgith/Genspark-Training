using NotificationApp.Interfaces;
using NotificationApp.Models;

namespace NotificationApp.Services;

internal class NotificationService
{
    private readonly List<User> users = new List<User>();
    private readonly List<Notification> notifications = new List<Notification>();

    public User CreateUser()
    {
        Console.WriteLine("Enter user details:");

        string name;
        while (true)
        {
            Console.Write("Name: ");
            name = Console.ReadLine()?.Trim() ?? string.Empty;
            if (Validation.IsNonEmptyName(name)) break;
            Console.WriteLine("Name cannot be empty. Please enter a valid name.");
        }

        string email;
        while (true)
        {
            Console.Write("Email: ");
            email = Console.ReadLine()?.Trim() ?? string.Empty;
            if (Validation.IsValidEmail(email)) break;
            Console.WriteLine("Invalid email format. Please enter a valid email.");
        }

        string phoneNo;
        while (true)
        {
            Console.Write("Phone number: ");
            phoneNo = Console.ReadLine()?.Trim() ?? string.Empty;
            if (Validation.IsValidPhone(phoneNo)) break;
            Console.WriteLine("Invalid phone number. Use digits and optional +, -, parentheses or spaces.");
        }

        User user = new User(name, email, phoneNo);
        users.Add(user);
        return user;
    }

    //Returns a read-only list of users to prevent external modification of the internal users list.
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

    //Sends a notification using the provided INotification implementation (Email or SMS) and stores the sent notification in the notifications list.
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
