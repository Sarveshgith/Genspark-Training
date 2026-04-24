using Bus_Booking_System.Models.Entities;
using Bus_Booking_System.Models.Enums;
using Microsoft.EntityFrameworkCore;
using RouteEntity = Bus_Booking_System.Models.Entities.Route;

namespace Bus_Booking_System.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<RouteEntity> Routes => Set<RouteEntity>();
    public DbSet<OperatorLocation> OperatorLocations => Set<OperatorLocation>();
    public DbSet<BusLayout> BusLayouts => Set<BusLayout>();
    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<SeatBooking> SeatBookings => Set<SeatBooking>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<UserRole>();
        modelBuilder.HasPostgresEnum<OperatorStatus>();
        modelBuilder.HasPostgresEnum<LocationStatus>();
        modelBuilder.HasPostgresEnum<RouteStatus>();
        modelBuilder.HasPostgresEnum<BusStatus>();
        modelBuilder.HasPostgresEnum<TripStatus>();
        modelBuilder.HasPostgresEnum<SeatBookingStatus>();
        modelBuilder.HasPostgresEnum<PassengerGender>();
        modelBuilder.HasPostgresEnum<TicketStatus>();
        modelBuilder.HasPostgresEnum<PaymentStatus>();

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
            entity.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(15).IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasColumnType("text").IsRequired();
            entity.Property(x => x.Role).HasColumnName("role").HasDefaultValue(UserRole.User).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Phone).IsUnique();
        });

        modelBuilder.Entity<Operator>(entity =>
        {
            entity.ToTable("operators");
            entity.HasKey(x => x.UserId);

            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.LicenseNumber).HasColumnName("license_number").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasDefaultValue(OperatorStatus.Pending).IsRequired();
            entity.Property(x => x.ApprovedBy).HasColumnName("approved_by");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasOne(x => x.User)
                .WithOne(x => x.OperatorProfile)
                .HasForeignKey<Operator>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ApprovedByUser)
                .WithMany(x => x.ApprovedOperators)
                .HasForeignKey(x => x.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.City).HasColumnName("city").HasMaxLength(100).IsRequired();
            entity.Property(x => x.State).HasColumnName("state").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasDefaultValue(LocationStatus.Pending).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasIndex(x => new { x.City, x.State }).IsUnique();
        });

        modelBuilder.Entity<RouteEntity>(entity =>
        {
            entity.ToTable("routes", t => t.HasCheckConstraint("ck_routes_from_to_different", "from_id <> to_id"));
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.FromId).HasColumnName("from_id").IsRequired();
            entity.Property(x => x.ToId).HasColumnName("to_id").IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasDefaultValue(RouteStatus.Active).IsRequired();
            entity.Property(x => x.CreatedBy).HasColumnName("created_by");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasIndex(x => new { x.FromId, x.ToId }).IsUnique();

            entity.HasOne(x => x.From)
                .WithMany(x => x.FromRoutes)
                .HasForeignKey(x => x.FromId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.To)
                .WithMany(x => x.ToRoutes)
                .HasForeignKey(x => x.ToId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CreatedByUser)
                .WithMany(x => x.CreatedRoutes)
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OperatorLocation>(entity =>
        {
            entity.ToTable("operator_locations");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OperatorId).HasColumnName("operator_id").IsRequired();
            entity.Property(x => x.LocationId).HasColumnName("location_id").IsRequired();
            entity.Property(x => x.Address).HasColumnName("address").HasColumnType("text").IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

            entity.HasOne(x => x.Operator)
                .WithMany(x => x.OperatorLocations)
                .HasForeignKey(x => x.OperatorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Location)
                .WithMany(x => x.OperatorLocations)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BusLayout>(entity =>
        {
            entity.ToTable("bus_layouts");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TotalSeats).HasColumnName("total_seats").IsRequired();
            entity.Property(x => x.Config).HasColumnName("config").HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Bus>(entity =>
        {
            entity.ToTable("buses");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OperatorId).HasColumnName("operator_id").IsRequired();
            entity.Property(x => x.LayoutId).HasColumnName("layout_id").IsRequired();
            entity.Property(x => x.VehicleNumber).HasColumnName("vehicle_number").HasMaxLength(20).IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasDefaultValue(BusStatus.Pending).IsRequired();
            entity.Property(x => x.ApprovedBy).HasColumnName("approved_by");
            entity.Property(x => x.ApprovedAt).HasColumnName("approved_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasIndex(x => x.VehicleNumber).IsUnique();

            entity.HasOne(x => x.Operator)
                .WithMany(x => x.Buses)
                .HasForeignKey(x => x.OperatorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Layout)
                .WithMany(x => x.Buses)
                .HasForeignKey(x => x.LayoutId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ApprovedByUser)
                .WithMany(x => x.ApprovedBuses)
                .HasForeignKey(x => x.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.ToTable("trips");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.BusId).HasColumnName("bus_id").IsRequired();
            entity.Property(x => x.RouteId).HasColumnName("route_id").IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasDefaultValue(TripStatus.Scheduled).IsRequired();
            entity.Property(x => x.DepartureTime).HasColumnName("departure_time").IsRequired();
            entity.Property(x => x.ArrivalTime).HasColumnName("arrival_time").IsRequired();
            entity.Property(x => x.PricePerSeat).HasColumnName("price_per_seat").HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasOne(x => x.Bus)
                .WithMany(x => x.Trips)
                .HasForeignKey(x => x.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Route)
                .WithMany(x => x.Trips)
                .HasForeignKey(x => x.RouteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.BookingRef).HasColumnName("booking_ref").HasMaxLength(50).IsRequired();
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.TripId).HasColumnName("trip_id").IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasDefaultValue(TicketStatus.Pending).IsRequired();
            entity.Property(x => x.BaseAmount).HasColumnName("base_amount").HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasDefaultValue(PaymentStatus.Pending).IsRequired();
            entity.Property(x => x.PaymentRef).HasColumnName("payment_ref").HasMaxLength(100);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasIndex(x => x.BookingRef).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.Tickets)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Trip)
                .WithMany(x => x.Tickets)
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SeatBooking>(entity =>
        {
            entity.ToTable("seat_bookings");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TicketId).HasColumnName("ticket_id").IsRequired();
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.TripId).HasColumnName("trip_id").IsRequired();
            entity.Property(x => x.SeatNumber).HasColumnName("seat_number").HasMaxLength(10).IsRequired();
            entity.Property(x => x.PassengerName).HasColumnName("passenger_name").HasMaxLength(100);
            entity.Property(x => x.PassengerAge).HasColumnName("passenger_age");
            entity.Property(x => x.PassengerGender).HasColumnName("passenger_gender");
            entity.Property(x => x.Status).HasColumnName("status").HasDefaultValue(SeatBookingStatus.Reserved).IsRequired();
            entity.Property(x => x.ReservedUntil).HasColumnName("reserved_until");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            entity.HasIndex(x => new { x.TripId, x.SeatNumber }).IsUnique();

            entity.HasOne(x => x.Ticket)
                .WithMany(x => x.SeatBookings)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.SeatBookings)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Trip)
                .WithMany(x => x.SeatBookings)
                .HasForeignKey(x => x.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
