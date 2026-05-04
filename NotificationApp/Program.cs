using System;
using NotificationApp.Models;
using NotificationApp.Services;

namespace NotificationApp;

internal class Program
{
	static void Main(string[] args)
	{
		var service = new NotificationService();

		while (true)
		{
			Console.WriteLine("\nNotification System");
			Console.WriteLine("1) Create user");
			Console.WriteLine("2) List users");
			Console.WriteLine("3) Send Email");
			Console.WriteLine("4) Send SMS");
			Console.WriteLine("5) Exit");
			Console.Write("Choose: ");
			var choice = Console.ReadLine();

			if (choice == "1")
			{
				var u = service.CreateUser();
				Console.WriteLine($"Created user: {u.Name}");
			}

			else if (choice == "2")
			{
				service.PrintUsers();
			}

			else if (choice == "3" || choice == "4")
			{
				var users = service.GetUsers();
				if (users.Count == 0)
				{
					Console.WriteLine("No users. Create one first.");
					continue;
				}
				Console.WriteLine("Select user index:");
				for (int i = 0; i < users.Count; i++) 
                    Console.WriteLine($"{i}) {users[i].Name}");

				Console.Write("Index: ");
				if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 0 || idx >= users.Count)
				{
					Console.WriteLine("Invalid index.");
					continue;
				}
				User user = users[idx];
				Console.Write("Message: ");
				string msg = Console.ReadLine() ?? string.Empty;
				Notification notif = new Notification { Message = msg, SentTime = DateTime.Now };

				if (choice == "3")
				{
					var email = new EmailNotification();
					service.SendNotification(email, user, notif);
				}
				else
				{
					var sms = new SMSNotification();
					service.SendNotification(sms, user, notif);
				}
			}
			else if (choice == "5") break;
            else Console.WriteLine("Invalid choice.");
		}
	}
}
