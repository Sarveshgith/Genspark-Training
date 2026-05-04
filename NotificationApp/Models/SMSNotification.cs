using System;
using NotificationApp.Interfaces;

namespace NotificationApp.Models;

internal class SMSNotification : INotification
{
    public void SendNotif(User user, Notification notification)
    {
        Console.WriteLine("-----------------------------");
        Console.WriteLine($"Sending SMS...");
        Console.WriteLine($"To: {user.PhoneNo}");
        Console.WriteLine($"Message: {notification.Message}");
        Console.WriteLine($"Sent at: {notification.SentTime}");
        Console.WriteLine($"SMS sent successfully!");
        Console.WriteLine("-----------------------------");
    }
}
