using NotificationApp.Models;
using NotificationApp.Services;
using NotificationApp.Contexts;
using NotificationApp.Repository;
using Microsoft.EntityFrameworkCore;
using NotificationApp.Presentation;

namespace NotificationApp;

internal class Program
{
	static void Main(string[] args)
	{
		// Create DbContext and apply migrations
		using var context = new NotifContext();
		context.Database.Migrate();

		// Wire up repositories with context and create service
		var userRepo = new UserRepository(context);
		var notifRepo = new NotificationRepository(context);
		var service = new NotificationService(userRepo, notifRepo);

		// Start interaction with user
		var interact = new InteractService(service);
		interact.StartInteraction();
	}
}
