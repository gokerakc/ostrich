using Microsoft.EntityFrameworkCore;
using Ostrich.Core.Models;

namespace Ostrich.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();

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

            entity.Property(p => p.Status)
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue("Pending");

            entity.HasIndex(p => p.Status);
            entity.HasIndex(p => p.CreatedAt);
        });
    }
}
