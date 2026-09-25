using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
    public DbSet<SavedParticular> SavedParticulars => Set<SavedParticular>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<WorkStage> WorkStages => Set<WorkStage>();
    public DbSet<QuotationWindow> QuotationWindows => Set<QuotationWindow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .Property(p => p.QuotationAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Product>()
            .Property(p => p.DiscountAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Product>()
            .Property(p => p.AdvancePayment).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Quotation>()
            .Property(q => q.DiscountValue).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Quotation>()
            .Property(q => q.DiscountAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Quotation>()
            .Property(q => q.SubTotal).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Quotation>()
            .Property(q => q.FinalAmount).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<QuotationItem>()
            .Property(qi => qi.Rate).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<QuotationItem>()
            .Property(qi => qi.Total).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<QuotationItem>()
            .HasOne(qi => qi.Quotation)
            .WithMany(q => q.Items)
            .HasForeignKey(qi => qi.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SavedParticular>()
            .Property(sp => sp.DefaultRate).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Quotation)
            .WithMany(q => q.Products)
            .HasForeignKey(p => p.QuotationId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Customer)
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Quotation>()
            .Property(q => q.AdvancePayment).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Quotation>()
            .HasOne(q => q.Customer)
            .WithMany(c => c.Quotations)
            .HasForeignKey(q => q.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuotationWindow>()
            .HasOne(w => w.Quotation)
            .WithMany(q => q.Windows)
            .HasForeignKey(w => w.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
        });
    }
}
