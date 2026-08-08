using Microsoft.EntityFrameworkCore;
using Ostrich.Core.Models;

namespace Ostrich.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Ledger> Ledgers => Set<Ledger>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            entity.Property(p => p.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(p => p.Merchant)
                .HasMaxLength(200)
                .IsRequired();
            
            entity.Property(p => p.AccountId)
                .IsRequired();

            entity.Property(p => p.Status)
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue("Pending");

            entity.HasOne<Account>()
                .WithMany()
                .HasForeignKey(p => p.AccountId);

            entity.HasIndex(p => p.ExternalId)
                .IsUnique();

            entity.HasIndex(p => p.Status);
            entity.HasIndex(p => p.CreatedAt);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(a => a.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(a => a.Balance)
                .HasColumnType("decimal(18,2)");

            entity.Property(a => a.Version)
                .IsConcurrencyToken()
                .IsRequired();
        });

        modelBuilder.Entity<Ledger>(entity =>
        {
            entity.ToTable("ledger");

            entity.HasKey(l => l.Id);

            entity.Property(l => l.Amount)
                .HasColumnType("decimal(18,2)");

            entity.Property(l => l.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.HasOne<Payment>()
                .WithMany()
                .HasForeignKey(l => l.PaymentId);

            entity.HasOne<Account>()
                .WithMany()
                .HasForeignKey(l => l.AccountId);

            entity.HasIndex(l => l.PaymentId);
            entity.HasIndex(l => l.AccountId);
            entity.HasIndex(l => l.CreatedAt);
        });
    }
}
