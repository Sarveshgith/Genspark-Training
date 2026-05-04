using System;
using NotificationApp.Interfaces;

namespace NotificationApp.Models;

internal class EmailNotification : INotification
{
    public void SendNotif(User user, Notification notification)
    {
        Console.WriteLine("-----------------------------");
        Console.WriteLine($"Sending email...");
        Console.WriteLine($"To: {user.Email}");
        Console.WriteLine($"Message: {notification.Message}");
        Console.WriteLine($"Sent at: {notification.SentTime}");
        Console.WriteLine($"Email sent successfully!");
        Console.WriteLine("-----------------------------");
    }
}
