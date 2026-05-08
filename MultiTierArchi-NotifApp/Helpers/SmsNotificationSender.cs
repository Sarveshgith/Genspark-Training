using System;
using MultiTierArchi_NotifApp.Interfaces;
using MultiTierArchi_NotifApp.Models;

namespace MultiTierArchi_NotifApp.Helpers;

internal class SmsNotificationSender : INotificationSender
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
