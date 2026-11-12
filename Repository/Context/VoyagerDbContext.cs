using Microsoft.EntityFrameworkCore;
using Repository.Models;

namespace Repository.Context;

/// <summary>
/// DbContext for all information required by Voyager.
/// </summary>
/// <param name="options">The DbContext options to use for voyager.</param>
public class VoyagerDbContext(DbContextOptions<VoyagerDbContext> options) : DbContext(options)
{
    /// <summary>
    /// All users known to the bot. Mapped to the <c>users</c> table.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Server> Servers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity => {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id)
                  .ValueGeneratedNever();

            entity.Property(u => u.Name)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(u => u.Settings)
                  .HasColumnType("text")
                  .IsRequired();

            entity.Property(u => u.CreatedAt)
                  .HasColumnType("datetime")
                  .IsRequired();
            entity.Property(u => u.UpdatedAt)
                  .HasColumnType("datetime")
                  .IsRequired();
            entity.Property(u => u.DeletedAt)
                  .HasColumnType("datetime");
        });

        modelBuilder.Entity<Server>(entity =>
        {
            entity.ToTable("servers");
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Id)
                  .ValueGeneratedNever();

            entity.Property(s => s.Name)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(s => s.Settings)
                  .HasColumnType("text")
                  .IsRequired();

            entity.Property(s => s.CreatedAt)
                  .HasColumnType("datetime")
                  .IsRequired();
            entity.Property(s => s.UpdatedAt)
                  .HasColumnType("datetime")
                  .IsRequired();
            entity.Property(s => s.DeletedAt)
                  .HasColumnType("datetime");
        });
    }
}
