using GrillMaster.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GrillMaster.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        // Constructor receives the database configuration from Program.cs
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Each DbSet represents a table in the database
        public DbSet<Category> Categories { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        // Database configuration and seed data
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Required for ASP.NET Core Identity tables
            base.OnModelCreating(modelBuilder);

            // Decimal precision
            modelBuilder.Entity<MenuItem>()
                .Property(m => m.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(o => o.UnitPrice)
                .HasPrecision(18, 2);

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Grilled Beef"
                },
                new Category
                {
                    Id = 2,
                    Name = "Grilled Chicken"
                },
                new Category
                {
                    Id = 3,
                    Name = "Grilled Seafood"
                }
            );

            // Seed Menu Items
            modelBuilder.Entity<MenuItem>().HasData(
                new MenuItem
                {
                    Id = 1,
                    Name = "Beef Steak",
                    Price = 500m,
                    CategoryId = 1
                },
                new MenuItem
                {
                    Id = 2,
                    Name = "Beef Kofta",
                    Price = 350m,
                    CategoryId = 1
                },
                new MenuItem
                {
                    Id = 3,
                    Name = "Grilled Chicken",
                    Price = 300m,
                    CategoryId = 2
                },
                new MenuItem
                {
                    Id = 4,
                    Name = "Seafood Platter",
                    Price = 800m,
                    CategoryId = 3
                },
                new MenuItem
                {
                    Id = 5,
                    Name = "Beef Skewers",
                    Price = 300m,
                    CategoryId = 1
                },
                new MenuItem
                {
                    Id = 6,
                    Name = "Double Burger",
                    Price = 250m,
                    CategoryId = 2
                },
                new MenuItem
                {
                    Id = 7,
                    Name = "Mixed Grill Platter",
                    Price = 650m,
                    CategoryId = 1
                },
                new MenuItem
                {
                    Id = 8,
                    Name = "Pizza",
                    Price = 250m,
                    CategoryId = 2
                },
                new MenuItem
                {
                    Id = 9,
                    Name = "Meatball Pasta",
                    Price = 300m,
                    CategoryId = 2
                },
                new MenuItem
                {
                    Id = 10,
                    Name = "Grilled Shrimp",
                    Price = 450m,
                    CategoryId = 3
                },
                new MenuItem
                {
                    Id = 11,
                    Name = "Grilled Chicken Wings",
                    Price = 250m,
                    CategoryId = 2
                }
            );
        }
    }
}