using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Models.Enums;

namespace OrderNKitchenMS_API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<MenuItemIngredient> MenuItemIngredients => Set<MenuItemIngredient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MenuItemIngredient>()
            .HasOne(mi => mi.MenuItem)
            .WithMany()
            .HasForeignKey(mi => mi.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MenuItemIngredient>()
            .HasOne(mi => mi.Item)
            .WithMany()
            .HasForeignKey(mi => mi.ItemId)
            .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<Role>()
            .HasData(
                new Role { Id = 1, Name = UserRole.Admin, CreatedAt = new DateTime(2026, 5, 28, 11, 4, 45, 313, DateTimeKind.Utc).AddTicks(3490) },
                new Role { Id = 2, Name = UserRole.Customer, CreatedAt = new DateTime(2026, 5, 28, 11, 4, 45, 313, DateTimeKind.Utc).AddTicks(3780) },
                new Role { Id = 3, Name = UserRole.Chef, CreatedAt = new DateTime(2026, 5, 28, 11, 4, 45, 313, DateTimeKind.Utc).AddTicks(3780) },
                new Role { Id = 4, Name = UserRole.Deliveryman, CreatedAt = new DateTime(2026, 5, 28, 11, 4, 45, 313, DateTimeKind.Utc).AddTicks(3780) },
                new Role { Id = 5, Name = UserRole.Waiter, CreatedAt = new DateTime(2026, 5, 28, 11, 4, 45, 313, DateTimeKind.Utc).AddTicks(3780) }
            );
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .Property(u => u.IsPending)
            .HasDefaultValue(true);

        modelBuilder.Entity<Table>()
            .HasIndex(table => table.Secret)
            .IsUnique();

        var isPostgres = Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";

        if (isPostgres)
        {
            modelBuilder.Entity<Table>()
                .HasIndex(table => table.Number)
                .HasFilter("\"IsDeleted\" = FALSE")
                .IsUnique();

            modelBuilder.Entity<MenuItem>()
                .HasIndex(mi => mi.Name)
                .HasFilter("\"IsDeleted\" = FALSE")
                .IsUnique();

            modelBuilder.Entity<Item>()
                .HasIndex(i => i.Name)
                .HasFilter("\"IsActive\" = TRUE")
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => new { c.Name, c.IsNonVeg })
                .HasFilter("\"IsDeleted\" = FALSE")
                .IsUnique();
        }
        else
        {
            modelBuilder.Entity<Table>()
                .HasIndex(table => table.Number)
                .HasFilter("IsDeleted = 0")
                .IsUnique();

            modelBuilder.Entity<MenuItem>()
                .HasIndex(mi => mi.Name)
                .HasFilter("IsDeleted = 0")
                .IsUnique();

            modelBuilder.Entity<Item>()
                .HasIndex(i => i.Name)
                .HasFilter("IsActive = 1")
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(c => new { c.Name, c.IsNonVeg })
                .HasFilter("IsDeleted = 0")
                .IsUnique();
        }

        modelBuilder.Entity<MenuItemIngredient>()
            .HasIndex(mii => new { mii.MenuItemId, mii.ItemId })
            .IsUnique();

        modelBuilder.Entity<MenuItem>()
            .HasOne(mi => mi.Category)
            .WithMany(category => category.MenuItems)
            .HasForeignKey(mi => mi.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.MenuItem)
            .WithMany()
            .HasForeignKey(oi => oi.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Table)
            .WithMany()
            .HasForeignKey(o => o.TableId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.AssignedChef)
            .WithMany()
            .HasForeignKey(o => o.AssignedChefId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.AssignedWaiter)
            .WithMany()
            .HasForeignKey(o => o.AssignedWaiterId)
            .OnDelete(DeleteBehavior.Restrict);

        if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                v => !v.HasValue ? v : (v.Value.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)),
                v => !v.HasValue ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }
        }
    }
}
