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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity => {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Name)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(u => u.Settings)
                  .HasColumnType("text")
                  .IsRequired();
        });
    }
}
