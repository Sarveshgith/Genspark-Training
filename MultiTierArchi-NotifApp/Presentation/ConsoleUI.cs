using System;
using MultiTierArchi_NotifApp.Helpers;
using MultiTierArchi_NotifApp.Models;
using MultiTierArchi_NotifApp.Models.Exceptions;
using MultiTierArchi_NotifApp.Services;

namespace MultiTierArchi_NotifApp.Presentation;

internal class ConsoleUI
{
	private readonly NotificationService notificationService = new NotificationService();
	private User? currentUser;
	private string? currentNotificationType;
	private string? currentMessage;
	private Notification? currentNotification;

	public void Run()
	{
		string? pendingChoice = null;

		while (true)
		{
			int choice;
			if (!string.IsNullOrEmpty(pendingChoice) && int.TryParse(pendingChoice, out int pending))
			{
				choice = pending;
				pendingChoice = null;
			}
			else
			{
				ShowMenu();
				string input = Console.ReadLine() ?? string.Empty;
				if (!int.TryParse(input.Trim(), out choice))
				{
					choice = -1;
				}
			}

			switch (choice)
			{
				case 1:
					AddOrSelectUser();
					if (PromptContinue())
					{
						pendingChoice = "2";
						continue;
					}
					break;

				case 2:
					ChooseNotificationType();
					if (PromptContinue())
					{
						pendingChoice = "3";
						continue;
					}
					break;

				case 3:
					CaptureNotificationMessage();
					if (PromptContinue())
					{
						pendingChoice = "4";
						continue;
					}
					break;

				case 4:
					SendNotification();
					if (PromptContinue())
					{
						pendingChoice = "5";
						continue;
					}
					break;

				case 5:
					DisplayCurrentDetails();
					break;

				case 6:
					notificationService.PrintUsers();
					break;

				case 7:
					notificationService.PrintNotifications();
					break;

				case 0:
					return;

				default:
					Console.WriteLine("Invalid menu choice.");
					break;
			}

			Console.WriteLine();
		}
	}

	private void AddOrSelectUser()
	{
		Console.WriteLine("1. Add user manually");
		Console.WriteLine("2. Choose user from existing list");
		Console.Write("Select: ");
		string addChoice = Console.ReadLine() ?? string.Empty;

		if (addChoice.Trim() == "2")
		{
			var users = notificationService.GetUsers();
			if (users.Count == 0)
			{
				Console.WriteLine("No users available. Please add a user manually.");
				return;
			}

			for (int i = 0; i < users.Count; i++)
			{
				Console.WriteLine($"{i + 1}. {users[i].Name} - {users[i].Email} - {users[i].PhoneNo}");
			}

			Console.Write("Enter the number of the user to select: ");
			string idxStr = Console.ReadLine() ?? string.Empty;
			if (int.TryParse(idxStr.Trim(), out int idx) && idx >= 1 && idx <= users.Count)
			{
				currentUser = users[idx - 1];
				Console.WriteLine("User selected.");
                Console.WriteLine("-----------------------------");
			}
			else
			{
				Console.WriteLine("Invalid selection.");
			}

			return;
		}

		while (true)
		{
			Console.Write("Name: ");
			string name = Console.ReadLine() ?? string.Empty;
			Console.Write("Email: ");
			string email = Console.ReadLine() ?? string.Empty;
			Console.Write("Phone number: ");
			string phoneNo = Console.ReadLine() ?? string.Empty;

			try
			{
				currentUser = notificationService.CreateUser(name, email, phoneNo);
				Console.WriteLine("User added successfully.");
                Console.WriteLine("-----------------------------");
				break;
			}
			catch (InvalidFormatException ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine("Please re-enter the user details.\n");
			}
		}
	}

	private void ChooseNotificationType()
	{
		Console.WriteLine("Choose notification type:");
		Console.WriteLine("1. Email");
		Console.WriteLine("2. SMS");
		Console.Write("Select: ");

		string notificationChoice = Console.ReadLine() ?? string.Empty;
		if (notificationChoice.Trim() == "1")
		{
			currentNotificationType = "Email";
			Console.WriteLine("Notification type set to Email.");
            Console.WriteLine("-----------------------------");
		}
		else if (notificationChoice.Trim() == "2")
		{
			currentNotificationType = "SMS";
			Console.WriteLine("Notification type set to SMS.");
            Console.WriteLine("-----------------------------");
		}
		else
		{
			Console.WriteLine("Invalid notification type choice.");
            Console.WriteLine("-----------------------------");
		}
	}

	private void CaptureNotificationMessage()
	{
		Console.Write("Enter notification message: ");
		currentMessage = Console.ReadLine();
		Console.WriteLine("Notification message captured.");
        Console.WriteLine("-----------------------------");
	}

	private void SendNotification()
	{
		try
		{
			if (currentUser == null)
			{
				throw new NotFoundException("Please add user details first.");
			}

			if (string.IsNullOrWhiteSpace(currentNotificationType))
			{
				throw new NotFoundException("Please choose a notification type first.");
			}

			if (currentMessage == null)
			{
				throw new NotFoundException("Please enter a notification message first.");
			}

            Notification notification;
            if(currentNotificationType == "Email"){
                var email = new EmailNotificationSender();
                notification = notificationService.CreateNotification(currentNotificationType, currentMessage);
                notificationService.SendNotification(email, currentUser, notification);
            }else{
                var sms = new SmsNotificationSender();
                notification = notificationService.CreateNotification(currentNotificationType, currentMessage);
                notificationService.SendNotification(sms, currentUser, notification);
            }
			currentNotification = notification;
			Console.WriteLine("Notification sent successfully.");
            Console.WriteLine("-----------------------------");
		}
		catch (NotFoundException ex)
		{
			Console.WriteLine(ex.Message);
            Console.WriteLine("-----------------------------");
		}
        catch (InvalidFormatException ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine("-----------------------------");
        }
	}

	private void DisplayCurrentDetails()
	{
		if (currentUser == null)
		{
			Console.WriteLine("No current user set.");
            Console.WriteLine("-----------------------------");
		}
		else
		{
			Console.WriteLine("Current User:");
			Console.WriteLine(currentUser);
            Console.WriteLine("-----------------------------");
		}

		if (currentNotification == null)
		{
			Console.WriteLine("No current notification available.");
            Console.WriteLine("-----------------------------");
		}
		else
		{
			Console.WriteLine("Current Notification:");
			Console.WriteLine(currentNotification);
            Console.WriteLine("-----------------------------");
		}
	}

	private static bool PromptContinue()
	{
		Console.Write("Continue to next operation? (y/N): ");
		string response = Console.ReadLine() ?? string.Empty;
        Console.WriteLine("-----------------------------");
		return response.Trim().ToLower() == "y";
	}

	private static void ShowMenu()
	{
		Console.WriteLine("=============================");
		Console.WriteLine("1. Add user details");
		Console.WriteLine("2. Choose notification type");
		Console.WriteLine("3. Enter notification message");
		Console.WriteLine("4. Send notification");
		Console.WriteLine("5. Display current user and notification details");
		Console.WriteLine("6. List users");
		Console.WriteLine("7. List notifications");
		Console.WriteLine("0. Exit");
		Console.Write("Select an option: ");
	}
}
