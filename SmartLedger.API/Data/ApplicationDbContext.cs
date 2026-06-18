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

            // 🔧 FIX: Add UserId indexes and relationships
            builder.Entity<Product>()
                .HasIndex(p => p.UserId)
                .HasDatabaseName("IX_Products_UserId");

            builder.Entity<InventoryTransaction>()
                .HasIndex(t => t.UserId)
                .HasDatabaseName("IX_InventoryTransactions_UserId");

            builder.Entity<Account>()
                .HasIndex(a => a.UserId)
                .HasDatabaseName("IX_Accounts_UserId");

            builder.Entity<JournalEntry>()
                .HasIndex(j => j.UserId)
                .HasDatabaseName("IX_JournalEntries_UserId");

            builder.Entity<TodoItem>()
                .HasIndex(t => t.UserId)
                .HasDatabaseName("IX_TodoItems_UserId");

            // 🔧 FIX: Configure Product-Transaction relationship
            builder.Entity<InventoryTransaction>()
                .HasOne(t => t.Product)
                .WithMany()
                .HasForeignKey(t => t.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

