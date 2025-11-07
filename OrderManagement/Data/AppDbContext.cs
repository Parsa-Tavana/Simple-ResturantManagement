using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Models;

namespace OrderManagement.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<MenuItem> MenuItems { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Restaurant> Restaurants { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ----- Customer -----
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // ----- Restaurant -----
            modelBuilder.Entity<Restaurant>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(300);

                entity.HasMany(r => r.MenuItems)
                      .WithOne(mi => mi.Restaurant)
                      .HasForeignKey(mi => mi.RestaurantId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ----- MenuItem -----
            modelBuilder.Entity<MenuItem>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.HasIndex(e => new { e.RestaurantId, e.Name });
            });

            // ----- Order -----
            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

                // NEW for Stripe
                entity.Property(e => e.PaidAt)                // nullable paid timestamp
                      .HasColumnType("datetime2");            // (default is fine; explicit for clarity)
                entity.Property(e => e.StripeSessionId)       // store Checkout Session id
                      .HasMaxLength(200);

                entity.HasOne(o => o.Customer)
                      .WithMany(c => c.Orders)
                      .HasForeignKey(o => o.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Restaurant)
                      .WithMany(r => r.Orders)
                      .HasForeignKey(o => o.RestaurantId)
                      .OnDelete(DeleteBehavior.Restrict);

                // 1-* Order -> OrderItems (cascade on delete)
                entity.HasMany(o => o.OrderItems)
                      .WithOne(oi => oi.Order)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Helpful indexes
                entity.HasIndex(o => o.CreatedAt);
                entity.HasIndex(o => o.Status);
                entity.HasIndex(o => new { o.RestaurantId, o.CreatedAt });
                entity.HasIndex(o => new { o.CustomerId, o.CreatedAt });
                entity.HasIndex(o => o.PaidAt);               // quick filter for paid/unpaid
                entity.HasIndex(o => o.StripeSessionId);      // lookup from webhook
            });

            // ----- OrderItem -----
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.NameSnapshot).IsRequired().HasMaxLength(200);
                // entity.Property(e => e.Quantity).HasDefaultValue(1); // optional
            });
        }
    }
}
