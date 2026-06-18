using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartLedger.API.Models;

namespace SmartLedger.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<TodoItem> TodoItems { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<JournalEntryLine> JournalEntryLines { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure decimal precision for PostgreSQL
            builder.Entity<Product>()
                .Property(p => p.CostPrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Product>()
                .Property(p => p.SellingPrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Account>()
                .Property(a => a.Balance)
                .HasColumnType("decimal(18,2)");

            builder.Entity<InventoryTransaction>()
                .Property(t => t.UnitPrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Account>()
                .HasIndex(a => a.UserId);

            builder.Entity<Product>()
                .HasIndex(p => p.UserId);

            builder.Entity<InventoryTransaction>()
                .HasIndex(t => t.UserId);

            builder.Entity<JournalEntry>()
                .HasIndex(j => j.UserId);

            builder.Entity<TodoItem>()
                .HasIndex(t => t.UserId);
        }
    }
}
