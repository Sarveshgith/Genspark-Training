using NotificationApp.Interfaces;
using NotificationApp.Models;
using NotificationApp.Repository;

namespace NotificationApp.Services;

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

    //Update User
    public void UpdateUser(string userEmail, User user)
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
        
        userRepository.Update(userEmail, user);
        Console.WriteLine("User updated successfully.");
    }

    //Delete User
    public void DeleteUser(string userEmail)
    {
        var deletedUser = userRepository.Delete(userEmail);
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
        Notification notification = new Notification { Message = message, SentTime = DateTime.Now };
        notificationRepository.Create(notification);
        return notification;
    }

    //Get All Notifications
    public IReadOnlyList<Notification> GetNotifications()
    {
        return notificationRepository.GetAll().AsReadOnly();
    }

    public void SendNotification(INotification notificationSender, User user, Notification notification)
    {
        notificationSender.SendNotif(user, notification);
        notificationRepository.Create(notification);
    }
}