using System;
using MultiTierArchi_NotifApp.Interfaces;
using MultiTierArchi_NotifApp.Models;

namespace MultiTierArchi_NotifApp.Helpers;

internal class EmailNotificationSender : INotificationSender
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
