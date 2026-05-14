using NotificationApp.Models;
using NotificationApp.Services;

namespace NotificationApp.Presentation;

internal class InteractService
{
    private readonly NotificationService _service;

    public InteractService(NotificationService service)
    {
        _service = service;
    }

    public void StartInteraction()
    {
        while (true)
        {
            Console.WriteLine("\nNotification System");
            Console.WriteLine("1) Create user");
            Console.WriteLine("2) List users");
            Console.WriteLine("3) Update user");
            Console.WriteLine("4) Delete user");
            Console.WriteLine("5) Send Email");
            Console.WriteLine("6) Send SMS");
            Console.WriteLine("7) Exit");
            Console.Write("Choose: ");
            var choice = Console.ReadLine();

            //Create User
            if (choice == "1")
            {
                var u = _service.CreateUser();
                Console.WriteLine($"Created user: {u.Name}");
            }

            //List Users
            else if (choice == "2")
            {
                _service.PrintUsers();
            }

            //Update User
            else if (choice == "3")
            {
                HandleUpdateUser();
            }

            //Delete User
            else if (choice == "4")
            {
                HandleDeleteUser();
            }

            //Send Notification
            else if (choice == "5" || choice == "6")
            {
                HandleSendNotification(choice);
            }
            else if (choice == "7") 
                break;
            else 
                Console.WriteLine("Invalid choice.");
        }
    }

    private void HandleUpdateUser()
    {
        var users = _service.GetUsers();
        if (users.Count == 0)
        {
            Console.WriteLine("No users. Create one first.");
            return;
        }
        Console.WriteLine("Select user index to update:");
        for (int i = 0; i < users.Count; i++)
            Console.WriteLine($"{i}) {users[i].Name}");

        Console.Write("Index: ");
        if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 0 || idx >= users.Count)
        {
            Console.WriteLine("Invalid index.");
            return;
        }
        _service.UpdateUser(users[idx].Id, users[idx]);
    }

    private void HandleDeleteUser()
    {
        var users = _service.GetUsers();
        if (users.Count == 0)
        {
            Console.WriteLine("No users. Create one first.");
            return;
        }
        Console.WriteLine("Select user index to delete:");
        for (int i = 0; i < users.Count; i++)
            Console.WriteLine($"{i}) {users[i].Name}");

        Console.Write("Index: ");
        if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 0 || idx >= users.Count)
        {
            Console.WriteLine("Invalid index.");
            return;
        }
        _service.DeleteUser(users[idx].Id);
    }

    private void HandleSendNotification(string choice)
    {
        var users = _service.GetUsers();
        if (users.Count == 0)
        {
            Console.WriteLine("No users. Create one first.");
            return;
        }
        Console.WriteLine("Select user index:");
        for (int i = 0; i < users.Count; i++)
            Console.WriteLine($"{i}) {users[i].Name}");

        Console.Write("Index: ");
        if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 0 || idx >= users.Count)
        {
            Console.WriteLine("Invalid index.");
            return;
        }
        User user = users[idx];
        Notification notif = _service.CreateNotification();

        if (choice == "5")
        {
            var email = new EmailNotification();
            _service.SendNotification(email, user, notif);
        }
        else
        {
            var sms = new SMSNotification();
            _service.SendNotification(sms, user, notif);
        }
    }
}
