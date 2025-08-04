using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Inventory.Api.Models;

namespace Inventory.Api.Data;

public class InventoryContext(DbContextOptions<InventoryContext> options) : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    #region DbSets

    public DbSet<Item> Items { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }

    #endregion

    #region Configuration Methods

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure each entity
        ConfigureApplicationUser(modelBuilder);
        ConfigureApplicationRole(modelBuilder);
        ConfigureEmployee(modelBuilder);
        ConfigureItem(modelBuilder);
        ConfigureInventoryTransaction(modelBuilder);

        // Configure Identity table names (optional - for cleaner naming)
        ConfigureIdentityTables(modelBuilder);
    }

    private static void ConfigureApplicationUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            // Table configuration
            entity.ToTable("Users");

            // Property configurations
            entity.Property(e => e.Firstname)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Department)
                .HasMaxLength(200);

            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt);

            // Indexes
            entity.HasIndex(e => e.EmployeeCode)
                .IsUnique()
                .HasFilter($"\"{nameof(ApplicationUser.EmployeeCode)}\" IS NOT NULL");

            entity.HasIndex(e => e.Email)
                .IsUnique();

            // Ignore computed properties
            entity.Ignore(e => e.FullName);
        });
    }

    private static void ConfigureApplicationRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationRole>(entity =>
        {
            // Table configuration
            entity.ToTable("Roles");

            // Property configurations
            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
        });
    }

    private static void ConfigureEmployee(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            // Table configuration
            entity.ToTable("Employees");

            // Primary key
            entity.HasKey(e => e.Id);

            // Property configurations
            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Position)
                .HasMaxLength(200);

            entity.Property(e => e.Department)
                .HasMaxLength(200);

            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(50);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt);

            // Indexes
            entity.HasIndex(e => new { e.FirstName, e.LastName })
                .IsUnique();

            // Ignore computed properties
            entity.Ignore(e => e.FullName);
        });
    }

    private static void ConfigureItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            // Table configuration
            entity.ToTable("Items", t =>
            {
                t.HasCheckConstraint("CK_Items_Quantity", "\"Quantity\" >= 0");
            });

            // Primary key
            entity.HasKey(e => e.Id);

            // Property configurations
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.Unit)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Quantity)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt);

            // Indexes
            entity.HasIndex(e => e.Name)
                .IsUnique();

            entity.HasIndex(e => e.IsActive);

            // Ignore computed properties
            entity.Ignore(e => e.CanReceiveStock);
            entity.Ignore(e => e.CanDistribute);
        });
    }

    private static void ConfigureInventoryTransaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            // Table configuration
            entity.ToTable("InventoryTransactions");

            // Primary key
            entity.HasKey(e => e.Id);

            // Property configurations
            entity.Property(e => e.TransactionCode)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Quantity)
                .IsRequired();

            entity.Property(e => e.TransactionType)
                .IsRequired()
                .HasConversion<string>(); // Store enum as string

            entity.Property(e => e.TransactionDate)
                .IsRequired(false); // Explicitly nullable for OUT transactions

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>(); // Store enum as string

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Remarks)
                .HasMaxLength(1000);

            // Foreign key relationships
            entity.HasOne(e => e.Item)
                .WithMany()
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReceivedByUser)
                .WithMany()
                .HasForeignKey(e => e.ReceivedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RequestedByEmployee)
                .WithMany()
                .HasForeignKey(e => e.RequestedByEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ProcessedByUser)
                .WithMany()
                .HasForeignKey(e => e.ProcessedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CancelledByUser)
                .WithMany()
                .HasForeignKey(e => e.CancelledByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(e => e.ItemId);
            entity.HasIndex(e => e.TransactionDate);
            entity.HasIndex(e => e.TransactionType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.ItemId, e.TransactionDate });
            entity.HasIndex(e => new { e.TransactionType, e.Status });
        });
    }

    private static void ConfigureIdentityTables(ModelBuilder modelBuilder)
    {
        // Customize Identity table names
        modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        modelBuilder.Entity<ApplicationRole>().ToTable("Roles");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");
    }

    #endregion

    #region Override Methods

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Item || e.Entity is ApplicationUser || e.Entity is Employee || e.Entity is InventoryTransaction)
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                if (entityEntry.Entity is Item item)
                    item.CreatedAt = DateTime.UtcNow;
                else if (entityEntry.Entity is ApplicationUser user)
                    user.CreatedAt = DateTime.UtcNow;
                else if (entityEntry.Entity is Employee employee)
                    employee.CreatedAt = DateTime.UtcNow;
                else if (entityEntry.Entity is InventoryTransaction transaction)
                    transaction.CreatedAt = DateTime.UtcNow;
            }

            if (entityEntry.State == EntityState.Modified)
            {
                if (entityEntry.Entity is Item item)
                    item.UpdatedAt = DateTime.UtcNow;
                else if (entityEntry.Entity is ApplicationUser user)
                    user.UpdatedAt = DateTime.UtcNow;
                else if (entityEntry.Entity is Employee employee)
                    employee.UpdatedAt = DateTime.UtcNow;
                // Note: InventoryTransaction doesn't have UpdatedAt - transactions are immutable
            }
        }
    }

    #endregion
}
