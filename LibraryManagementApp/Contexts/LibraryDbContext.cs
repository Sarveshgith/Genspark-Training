using LibraryManagementApp.Models;
using Microsoft.EntityFrameworkCore;
using LibraryManagementApp.Enums;

namespace LibraryManagementApp.Contexts;

internal class LibraryDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Username=sarvesh;Password=Sarvesh_dev@1;Database=library_app");
    }

    public DbSet<Member> Members { get; set; }
    public DbSet<Membership> Memberships { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<BookCopy> BookCopies { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Borrow> Borrows { get; set; }
    public DbSet<Fine> Fines { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var seedDate = DateTime.SpecifyKind(
                new DateTime(2026, 5, 17),DateTimeKind.Utc);

        //Create a admin user if not exists
        modelBuilder.Entity<Member>()
            .HasData(new Member
            {
                Id = 1,
                Username = "admin",
                Password = "admin123",
                Name = "Admin User",
                Email = "admin@library.com",
                PhoneNo = "1234567890",
                MembershipId = 1,
                Status = MemberStatus.Active,
                CreatedAt = seedDate 
            });

        // Seed Memberships
        modelBuilder.Entity<Membership>()
            .HasData(
                new Membership { Id = 1, Type = MembershipType.Basic, MaxBrwDays = 7, MaxBrwBooks = 2, CreatedAt = seedDate },
                new Membership { Id = 2, Type = MembershipType.Student, MaxBrwDays = 10, MaxBrwBooks = 3, CreatedAt = seedDate },
                new Membership { Id = 3, Type = MembershipType.Premium, MaxBrwDays = 15, MaxBrwBooks = 5, CreatedAt = seedDate }
            );

            
        base.OnModelCreating(modelBuilder);

        // Unique Constraints
        modelBuilder.Entity<Member>()
            .HasIndex(m => m.Username)
            .IsUnique();

        modelBuilder.Entity<Member>()
            .HasIndex(m => m.Email)
            .IsUnique();

        modelBuilder.Entity<Member>()
            .HasIndex(m => m.PhoneNo)
            .IsUnique();

        modelBuilder.Entity<Book>()
            .HasIndex(b => b.ISBN)
            .IsUnique();

        // Member -> Membership
        modelBuilder.Entity<Member>()
            .HasOne(m => m.Membership)
            .WithMany()
            .HasForeignKey(m => m.MembershipId)
            .OnDelete(DeleteBehavior.Restrict);

        // Book -> Category
        modelBuilder.Entity<Book>()
            .HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // BookCopy -> Book
        modelBuilder.Entity<BookCopy>()
            .HasOne(bc => bc.Book)
            .WithMany()
            .HasForeignKey(bc => bc.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        // Borrow -> Member
        modelBuilder.Entity<Borrow>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Borrow -> Book
        modelBuilder.Entity<Borrow>()
            .HasOne(b => b.Book)
            .WithMany()
            .HasForeignKey(b => b.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        // Borrow -> BookCopy
        modelBuilder.Entity<Borrow>()
            .HasOne(b => b.BookCopy)
            .WithMany()
            .HasForeignKey(b => b.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Fine -> Member
        modelBuilder.Entity<Fine>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Fine -> Borrow
        modelBuilder.Entity<Fine>()
            .HasOne(f => f.Borrow)
            .WithMany()
            .HasForeignKey(f => f.BorrowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}