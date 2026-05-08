using System;
using System.Reflection;
using MultiTierArchi_NotifApp.Interfaces;
using MultiTierArchi_NotifApp.Models;
using MultiTierArchi_NotifApp.Repositories;
using MultiTierArchi_NotifApp.Helpers;

namespace MultiTierArchi_NotifApp.Services;

internal class NotificationService
{
    public readonly UserRepository userRepository;
    public readonly NotificationRepository notificationRepository;

    public NotificationService()
    {
        userRepository = new UserRepository();
        notificationRepository = new NotificationRepository();
    }

    //-------------------USER METHODS--------------------

    //Create User
    public User CreateUser(string name, string email, string phoneNo)
    {
        if (!Validation.IsNonEmptyName(name)) throw new InvalidFormatException("Name cannot be empty.");
        if (!Validation.IsValidEmail(email)) throw new InvalidFormatException("User email is invalid.");
        if (!Validation.IsValidPhone(phoneNo)) throw new InvalidFormatException("User phone number is invalid.");

        User user = new User(name.Trim(), email.Trim(), phoneNo.Trim());
        userRepository.Create(user);
        return user;
    }

    //Get All Users
    public IReadOnlyList<User> GetUsers()
    {
        return userRepository.GetAll().AsReadOnly();
    }

    //Print All Users
    public void PrintUsers()
    {
        var users = userRepository.GetAll();
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

    private void PrintUser(User user)
    {
        Console.WriteLine("-----------------------------");
        Console.WriteLine(user);
        Console.WriteLine("-----------------------------");
    }

    //-------------------NOTIFICATION METHODS--------------------

    //Create Notification via arguments with validation.
    public Notification CreateNotification(string notifType, string message)
    {
        if (!Validation.IsSupportedNotificationType(notifType))
            throw new InvalidFormatException("Notification type must be Email or SMS.");

        if (!Validation.IsValidMessage(message))
            throw new InvalidFormatException("Message should not be empty and must be at least 5 characters.");

        Notification notification = new Notification
        {
            NotifType = notifType.Trim(),
            Message = message.Trim(),
            SentTime = DateTime.Now
        };

        return notification;
    }

    //Get All Notifications
    public IReadOnlyList<Notification> GetNotifications()
    {
        return notificationRepository.GetAll().AsReadOnly();
    }

    public void PrintNotifications()
    {
        var notifications = notificationRepository.GetAll();
        if (notifications.Count == 0)
        {
            Console.WriteLine("No notifications.");
            return;
        }

        foreach (var notification in notifications)
        {
            Console.WriteLine("-----------------------------");
            Console.WriteLine(notification);
            Console.WriteLine("-----------------------------");
        }
    }

    public void SendNotification(INotificationSender notificationSender, User user, Notification notification)
    {
        notificationSender.SendNotif(user, notification);
        notificationRepository.Create(notification);
    }
}