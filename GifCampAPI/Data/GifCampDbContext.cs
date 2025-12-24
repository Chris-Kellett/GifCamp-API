using Microsoft.EntityFrameworkCore;
using GifCampAPI.Models;

namespace GifCampAPI.Data;

public class GifCampDbContext : DbContext
{
    public GifCampDbContext(DbContextOptions<GifCampDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Picture).HasMaxLength(500);
            entity.Property(e => e.Method).IsRequired().HasMaxLength(50);
            
            // Create unique index on Email + Method combination
            entity.HasIndex(e => new { e.Email, e.Method }).IsUnique();
        });
    }
}

