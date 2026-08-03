using Microsoft.EntityFrameworkCore;
using Ostrich.Core.Models;

namespace Ostrich.Core.Data;

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
            entity.HasIndex(p => p.Status);
            entity.HasIndex(p => p.CreatedAt);
        });
    }
}
