using NotificationApp.Models;
using NotificationApp.Services;
using NotificationApp.Data;

namespace NotificationApp;

//Do Refer Documentation for the total workflow and DesignModel.
internal class Program
{
	static void Main(string[] args)
	{
		DatabaseInitializer.Initialize();

		var service = new NotificationService();

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
				var u = service.CreateUser();
				Console.WriteLine($"Created user: {u.Name}");
			}

			//List Users
			else if (choice == "2")
			{
				service.PrintUsers();
			}

			//Update User
			else if (choice == "3")
			{
				var users = service.GetUsers();
				if (users.Count == 0)
				{
					Console.WriteLine("No users. Create one first.");
					continue;
				}
				Console.WriteLine("Select user index to update:");
				for (int i = 0; i < users.Count; i++) 
                    Console.WriteLine($"{i}) {users[i].Name}");

				Console.Write("Index: ");
				if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 0 || idx >= users.Count)
				{
					Console.WriteLine("Invalid index.");
					continue;
				}
				service.UpdateUser(users[idx].Id, users[idx]);
			}

			//Delete User
			else if (choice == "4")
			{
				var users = service.GetUsers();
				if (users.Count == 0)
				{
					Console.WriteLine("No users. Create one first.");
					continue;
				}
				Console.WriteLine("Select user index to delete:");
				for (int i = 0; i < users.Count; i++) 
                    Console.WriteLine($"{i}) {users[i].Name}");

				Console.Write("Index: ");
				if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 0 || idx >= users.Count)
				{
					Console.WriteLine("Invalid index.");
					continue;
				}
				service.DeleteUser(users[idx].Id);
			}

			//Send Notification
			else if (choice == "5" || choice == "6")
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
				Notification notif = service.CreateNotification();

				if (choice == "5")
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
			else if (choice == "7") break;
            else Console.WriteLine("Invalid choice.");
		}
	}
}