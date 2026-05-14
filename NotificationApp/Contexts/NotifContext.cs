using NotificationApp.Models;
using Microsoft.EntityFrameworkCore;

namespace NotificationApp.Contexts;

internal class NotifContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Username=sarvesh;Password=Sarvesh_dev@1;Database=notif_app");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User entity
        modelBuilder.Entity<User>(u =>
        {
            u.HasKey(user => user.Id);
            u.Property(user => user.Name).IsRequired().HasMaxLength(255);
            u.Property(user => user.Email).IsRequired().HasMaxLength(255);
            u.HasIndex(user => user.Email).IsUnique();
            u.Property(user => user.PhoneNo).IsRequired().HasMaxLength(20);
            u.HasIndex(user => user.PhoneNo).IsUnique();
        });

        // Notification entity
        modelBuilder.Entity<Notification>(n =>
        {
            n.HasKey(notif => notif.Id);
            n.Property(notif => notif.UserId).IsRequired();
            n.Property(notif => notif.Message).IsRequired();
            n.Property(notif => notif.NotifType).IsRequired().HasMaxLength(20);
            n.Property(notif => notif.SentDate).IsRequired();
        });

        // Relationship: User -> Notifications
        modelBuilder.Entity<Notification>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}