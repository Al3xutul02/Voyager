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

    /// <summary>
    /// All Discord servers (guilds) configured for the bot. Mapped to the <c>servers</c> table.
    /// </summary>
    public DbSet<Server> Servers { get; set; } = null!;

    /// <summary>
    /// All saved user dice roll configurations. Mapped to the <c>dice_rolls</c> table.
    /// </summary>
    public DbSet<DiceRoll> DiceRolls { get; set; } = null!;

    /// <summary>
    /// All user characters. Mapped to the <c>characters</c> table.
    /// </summary>
    public DbSet<Character> Characters { get; set; } = null!;

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

            entity.Property(s => s.Description)
                  .HasColumnType("text")
                  .HasDefaultValue(null);

            entity.Property(s => s.GMRoleId)
                  .IsRequired();

            entity.Property(s => s.AdminChannelId)
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

        modelBuilder.Entity<DiceRoll>(entity =>
        {
            entity.ToTable("dice_rolls");
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Id)
                  .ValueGeneratedOnAdd();

            entity.Property(d => d.UserId)
                  .IsRequired();

            entity.HasOne(d => d.User)
                  .WithMany(u => u.DiceRolls)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(d => d.Name)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(d => d.Formula)
                  .HasColumnType("text")
                  .IsRequired();

            entity.Property(d => d.CreatedAt)
                  .HasColumnType("datetime")
                  .IsRequired();
            entity.Property(d => d.UpdatedAt)
                  .HasColumnType("datetime")
                  .IsRequired();
            entity.Property(d => d.DeletedAt)
                  .HasColumnType("datetime");
        });

        modelBuilder.Entity<Character>(entity =>
        {
            entity.ToTable("characters");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                  .ValueGeneratedOnAdd();

            entity.Property(c => c.UserId)
                  .IsRequired();

            entity.HasOne(c => c.User)
                  .WithMany(u => u.Characters)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(c => c.Name)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(c => c.Description)
                  .HasColumnType("text");

            entity.Property(c => c.Color)
                  .IsRequired();

            entity.Property(c => c.TokenImagePath)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(c => c.ActorImagePath)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(c => c.CurrentGame)
                  .HasMaxLength(100);

            entity.Property(c => c.Gold)
                  .HasColumnType("decimal(10,2)")
                  .IsRequired();

            entity.Property(c => c.PathbuilderId)
                  .HasMaxLength(50);

            entity.Property(c => c.Details)
                  .HasColumnType("text")
                  .IsRequired();

            entity.Property(c => c.CreatedAt)
                  .HasColumnType("datetime")
                  .IsRequired();
            entity.Property(c => c.UpdatedAt)
                  .HasColumnType("datetime")
                  .IsRequired();
            entity.Property(c => c.DeletedAt)
                  .HasColumnType("datetime");

            entity.HasIndex(c => new { c.UserId, c.Name });
        });
    }
}
