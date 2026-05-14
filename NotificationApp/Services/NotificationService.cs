using NotificationApp.Interfaces;
using NotificationApp.Models;
using NotificationApp.Repository;
using System.Linq;

namespace NotificationApp.Services;

internal class NotificationService
{
    public readonly IUserRepository userRepository;
    public readonly INotificationRepository notificationRepository;

    public NotificationService(IUserRepository userRepo, INotificationRepository notifRepo)
    {
        userRepository = userRepo;
        notificationRepository = notifRepo;
    }

    //-------------------USER METHODS--------------------

    //Create User
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

        if (userRepository.GetByEmail(user.Email) != null)
        {
            Console.WriteLine("User with this email already exists.");
            return user;
        }

        if (userRepository.GetByPhone(user.PhoneNo) != null)
        {
            Console.WriteLine("User with this phone number already exists.");
            return user;
        }

        userRepository.Create(user);
        return user;
    }

    //Get All Users
    public IReadOnlyList<User> GetUsers()
    {
        return userRepository.GetAll()
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Email)
            .ToList()
            .AsReadOnly();
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

    //Update User
    public void UpdateUser(int userId, User user)
    {
        Console.WriteLine("Update user details:");

        string name;
        while (true)
        {
            Console.Write("Name (current: " + user.Name + "): ");
            name = Console.ReadLine()?.Trim() ?? user.Name;
            if (Validation.IsNonEmptyName(name)) break;
            Console.WriteLine("Name cannot be empty. Please enter a valid name.");
        }

        string email;
        while (true)
        {
            Console.Write("Email (current: " + user.Email + "): ");
            email = Console.ReadLine()?.Trim() ?? user.Email;
            if (Validation.IsValidEmail(email)) break;
            Console.WriteLine("Invalid email format. Please enter a valid email.");
        }

        string phoneNo;
        while (true)
        {
            Console.Write("Phone number (current: " + user.PhoneNo + "): ");
            phoneNo = Console.ReadLine()?.Trim() ?? user.PhoneNo;
            if (Validation.IsValidPhone(phoneNo)) break;
            Console.WriteLine("Invalid phone number. Use digits and optional +, -, parentheses or spaces.");
        }

        user.Name = name;
        user.Email = email;
        user.PhoneNo = phoneNo;
        
        var existingByEmail = userRepository.GetByEmail(email);
        if (existingByEmail != null && existingByEmail.Id != userId)
        {
            Console.WriteLine("Another user already has this email.");
            return;
        }

        var existingByPhone = userRepository.GetByPhone(phoneNo);
        if (existingByPhone != null && existingByPhone.Id != userId)
        {
            Console.WriteLine("Another user already has this phone number.");
            return;
        }

        var updated = userRepository.Update(userId, user);
        Console.WriteLine(updated != null ? "User updated successfully." : "User not found.");
    }

    //Delete User
    public void DeleteUser(int userId)
    {
        var deletedUser = userRepository.Delete(userId);
        if (deletedUser != null)
        {
            Console.WriteLine($"User {deletedUser.Name} deleted successfully.");
        }
        else
        {
            Console.WriteLine("User not found.");
        }
    }

    //-------------------NOTIFICATION METHODS--------------------

    //Create Notification
    public Notification CreateNotification()
    {
        Console.Write("Enter notification message: ");
        string message = Console.ReadLine() ?? string.Empty;
        Notification notification = new Notification { Message = message, SentDate = DateTime.Now };
        return notification;
    }

    //Get All Notifications
    public IReadOnlyList<Notification> GetNotifications()
    {
        return notificationRepository.GetAll().AsReadOnly();
    }

    public void SendNotification(INotification notificationSender, User user, Notification notification)
    {
        notification.UserId = user.Id;
        notification.NotifType = notificationSender is EmailNotification ? "Email" : "SMS";
        notification.SentDate = DateTime.Now;
        notificationSender.SendNotif(user, notification);
        notificationRepository.Create(notification);
    }

    public User? GetUserByEmail(string email)
    {
        return userRepository.GetByEmail(email);
    }

    public User? GetUserByPhone(string phoneNo)
    {
        return userRepository.GetByPhone(phoneNo);
    }
}