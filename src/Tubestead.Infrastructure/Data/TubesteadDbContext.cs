using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tubestead.Domain.Entities;
using Tubestead.Infrastructure.Identity;

namespace Tubestead.Infrastructure.Data;

/// <summary>EF Core context for Tubestead. Inherits Identity's user/role tables
/// and adds the media + settings model. Provider (SQLite / SQL Server) is chosen
/// at registration time, so this class stays provider-agnostic.</summary>
public class TubesteadDbContext(DbContextOptions<TubesteadDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<MediaRendition> MediaRenditions => Set<MediaRendition>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistItem> PlaylistItems => Set<PlaylistItem>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<AppSetting>(e =>
        {
            e.HasKey(s => s.Key);
            e.Property(s => s.Key).HasMaxLength(128);
        });

        b.Entity<Video>(e =>
        {
            e.Property(v => v.Title).HasMaxLength(512).IsRequired();
            e.HasIndex(v => v.OwnerId);
            e.HasIndex(v => v.Status);
            e.HasIndex(v => v.CreatedUtc);

            e.HasMany(v => v.Renditions)
                .WithOne(r => r.Video!)
                .HasForeignKey(r => r.VideoId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(v => v.Tags)
                .WithMany(t => t.Videos)
                .UsingEntity(j => j.ToTable("VideoTags"));
        });

        b.Entity<Tag>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(128).IsRequired();
            e.Property(t => t.Slug).HasMaxLength(128).IsRequired();
            e.HasIndex(t => t.Slug).IsUnique();
        });

        b.Entity<Playlist>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(256).IsRequired();
            e.HasIndex(p => p.OwnerId);

            e.HasMany(p => p.Items)
                .WithOne(i => i.Playlist!)
                .HasForeignKey(i => i.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PlaylistItem>(e =>
        {
            e.HasIndex(i => new { i.PlaylistId, i.Position });
            e.HasOne(i => i.Video)
                .WithMany(v => v.PlaylistItems)
                .HasForeignKey(i => i.VideoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SQLite can't compare/ORDER BY DateTimeOffset (stored as TEXT). Persist all
        // DateTimeOffset values as UTC ticks (a sortable INTEGER) on that provider.
        // SQL Server has native DateTimeOffset support, so leave it untouched there.
        if (Database.IsSqlite())
        {
            var converter = new ValueConverter<DateTimeOffset, long>(
                v => v.UtcTicks,
                v => new DateTimeOffset(v, TimeSpan.Zero));
            var nullableConverter = new ValueConverter<DateTimeOffset?, long?>(
                v => v.HasValue ? v.Value.UtcTicks : null,
                v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null);

            foreach (var entityType in b.Model.GetEntityTypes())
                foreach (var prop in entityType.GetProperties())
                {
                    if (prop.ClrType == typeof(DateTimeOffset))
                        prop.SetValueConverter(converter);
                    else if (prop.ClrType == typeof(DateTimeOffset?))
                        prop.SetValueConverter(nullableConverter);
                }
        }
    }
}
